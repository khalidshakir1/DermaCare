using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class BillingService : IBillingService
{
    private readonly IBillingRepository _billingRepository;
    private readonly ICurrentSession _session;
    private readonly IInventoryService? _inventoryService;

    public BillingService(
        IBillingRepository billingRepository,
        ICurrentSession session,
        IInventoryService? inventoryService = null)
    {
        _billingRepository = billingRepository;
        _session = session;
        _inventoryService = inventoryService;
    }

    public Task<IReadOnlyList<ServiceItem>> GetActiveServicesAsync(CancellationToken ct = default) =>
        _billingRepository.GetActiveServicesAsync(ct);

    public Task<IReadOnlyList<Invoice>> GetInvoicesByPatientAsync(int patientId, CancellationToken ct = default) =>
        _billingRepository.GetInvoicesByPatientAsync(patientId, ct);

    public Task<IReadOnlyList<Invoice>> GetRecentInvoicesAsync(int take = 50, CancellationToken ct = default) =>
        _billingRepository.GetRecentInvoicesAsync(take, ct);

    public Task<decimal> GetTodaysRevenueAsync(CancellationToken ct = default) =>
        _billingRepository.GetTodaysRevenueAsync(ct);

    public Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default) =>
        _billingRepository.GetClinicProfileAsync(ct);

    public async Task<ServiceResult<Invoice>> CreateInvoiceAsync(
        int patientId,
        int? appointmentId,
        IEnumerable<InvoiceItem> items,
        string? notes = null,
        decimal initialPayment = 0m,
        PaymentMethod initialPaymentMethod = PaymentMethod.Cash,
        decimal discountAmount = 0m,
        decimal discountPercent = 0m,
        CancellationToken ct = default)
    {
        if (patientId <= 0)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Please select a patient.");

        var itemList = items.Where(i => i.Quantity > 0 && i.UnitPrice >= 0).ToList();
        if (itemList.Count == 0)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Add at least one bill item.");

        if (initialPayment < 0)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Initial payment cannot be negative.");

        if (discountAmount < 0 || discountPercent < 0 || discountPercent > 100)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Invalid discount value.");

        // Validate medicine stock before creating the bill
        var medicineItems = itemList.Where(i => i.MedicineId.HasValue).ToList();
        if (medicineItems.Count > 0 && _inventoryService is not null)
        {
            var medicines = await _inventoryService.GetMedicinesAsync(includeInactive: true, ct: ct);
            var byId = medicines.ToDictionary(m => m.MedicineId);

            foreach (var group in medicineItems.GroupBy(i => i.MedicineId!.Value))
            {
                var requestedQty = group.Sum(i => i.Quantity);
                if (!byId.TryGetValue(group.Key, out var med))
                    return new ServiceResult<Invoice>(false, ErrorMessage: $"Medicine ID {group.Key} was not found in inventory.");

                if (med.CurrentStock < requestedQty)
                    return new ServiceResult<Invoice>(
                        false,
                        ErrorMessage: $"Insufficient stock for {med.Name}. Available: {med.CurrentStock}, Requested: {requestedQty}.");
            }
        }

        var subtotal = itemList.Sum(i => i.Quantity * i.UnitPrice);
        var computedDiscount = discountAmount + (subtotal * discountPercent / 100m);
        if (computedDiscount > subtotal) computedDiscount = subtotal;
        var total = Math.Max(0m, subtotal - computedDiscount);

        var billNumber = await _billingRepository.GenerateNextBillNumberAsync(ct);
        if (string.IsNullOrWhiteSpace(billNumber))
            billNumber = $"BILL-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var invoice = new Invoice
        {
            BillNumber = billNumber,
            PatientId = patientId,
            AppointmentId = appointmentId,
            Subtotal = subtotal,
            DiscountAmount = computedDiscount,
            DiscountPercent = discountPercent,
            TotalAmount = total,
            PaidAmount = 0m,
            Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Items = itemList
        };

        await _billingRepository.AddInvoiceAsync(invoice, ct);

        // Auto-deduct medicines from inventory
        if (medicineItems.Count > 0 && _inventoryService is not null)
        {
            var userId = _session.CurrentUser?.UserId ?? 0;
            foreach (var group in medicineItems.GroupBy(i => i.MedicineId!.Value))
            {
                var qty = group.Sum(i => i.Quantity);
                await _inventoryService.AdjustOrDispenseAsync(
                    group.Key,
                    StockTransactionType.Dispensed,
                    qty,
                    $"Billed on {invoice.BillNumber}",
                    userId,
                    ct);
            }
        }

        if (initialPayment > 0)
        {
            var payResult = await RecordPaymentAsync(invoice.InvoiceId, initialPayment, initialPaymentMethod, ct);
            if (payResult.Success && payResult.Data is not null)
                invoice = payResult.Data;
        }

        return new ServiceResult<Invoice>(true, invoice);
    }

    public async Task<ServiceResult<Invoice>> RecordPaymentAsync(
        int invoiceId,
        decimal amount,
        PaymentMethod method,
        CancellationToken ct = default)
    {
        if (amount <= 0)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Payment amount must be greater than zero.");

        var invoice = await _billingRepository.GetInvoiceByIdAsync(invoiceId, ct);
        if (invoice is null)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Bill not found.");

        if (invoice.Status == InvoiceStatus.Void)
            return new ServiceResult<Invoice>(false, ErrorMessage: "Cannot record payment on a void bill.");

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            Method = method,
            PaidAt = DateTime.UtcNow,
            ReceivedBy = _session.CurrentUser?.UserId ?? 0
        };

        await _billingRepository.AddPaymentAsync(payment, ct);

        invoice.PaidAmount += amount;
        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _billingRepository.UpdateInvoiceAsync(invoice, ct);
        return new ServiceResult<Invoice>(true, invoice);
    }
}

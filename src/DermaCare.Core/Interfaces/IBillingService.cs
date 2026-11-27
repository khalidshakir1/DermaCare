using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IBillingService
{
    Task<IReadOnlyList<ServiceItem>> GetActiveServicesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetInvoicesByPatientAsync(int patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetRecentInvoicesAsync(int take = 50, CancellationToken ct = default);
    Task<decimal> GetTodaysRevenueAsync(CancellationToken ct = default);
    Task<decimal> GetTodaysRevenueByUserAsync(int userId, CancellationToken ct = default);
    Task<int> GetTodaysBillCountAsync(int? userId = null, CancellationToken ct = default);
    Task<decimal> GetTodaysDiscountsAsync(CancellationToken ct = default);
    Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default);
    Task<ServiceResult<Invoice>> CreateInvoiceAsync(
        int? patientId,
        int? appointmentId,
        IEnumerable<InvoiceItem> items,
        string? notes = null,
        decimal initialPayment = 0m,
        PaymentMethod initialPaymentMethod = PaymentMethod.Cash,
        decimal discountAmount = 0m,
        decimal discountPercent = 0m,
        string? discountReason = null,
        string? customerName = null,
        CancellationToken ct = default);
    Task<ServiceResult<Invoice>> RecordPaymentAsync(int invoiceId, decimal amount, PaymentMethod method, CancellationToken ct = default);
}

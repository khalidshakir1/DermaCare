using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class ReportService : IReportService
{
    private readonly DermaDbContext _context;

    public ReportService(DermaDbContext context) => _context = context;

    public async Task<ClinicReportSummary> GenerateSummaryAsync(
        DateTime from,
        DateTime to,
        int? doctorId = null,
        CancellationToken ct = default)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);
        var utcStart = start.ToUniversalTime();
        var utcEnd = end.ToUniversalTime();

        var apptQuery = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end);

        if (doctorId.HasValue)
            apptQuery = apptQuery.Where(a => a.DoctorId == doctorId.Value);

        var appointments = await apptQuery.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToListAsync(ct);

        var invoices = await _context.Invoices
            .Include(i => i.Patient)
            .Where(i => i.IssuedAt >= utcStart && i.IssuedAt < utcEnd && i.Status != InvoiceStatus.Void)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync(ct);

        var payments = await _context.Payments
            .Where(p => p.PaidAt >= utcStart && p.PaidAt < utcEnd)
            .ToListAsync(ct);

        var revenueByMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodSummary(g.Key, g.Sum(p => p.Amount), g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var newPatients = await _context.Patients
            .CountAsync(p => p.CreatedAt >= utcStart && p.CreatedAt < utcEnd, ct);

        var users = await _context.Users
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        var discountsByUser = invoices
            .GroupBy(i => i.CollectedByUserId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.DiscountAmount));

        var billCountsByUser = invoices
            .GroupBy(i => i.CollectedByUserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var revenueByReceptionist = payments
            .GroupBy(p => p.ReceivedBy)
            .Select(g => new ReceptionistRevenueSummary(
                UserId: g.Key,
                ReceptionistName: g.Key > 0 && users.TryGetValue(g.Key, out var name) ? name : "System / Walk-in",
                TotalCollected: g.Sum(p => p.Amount),
                BillCount: billCountsByUser.TryGetValue(g.Key, out var cnt) ? cnt : g.Select(p => p.InvoiceId).Distinct().Count(),
                TotalDiscount: discountsByUser.TryGetValue(g.Key, out var disc) ? disc : 0m))
            .OrderByDescending(r => r.TotalCollected)
            .ToList();

        return new ClinicReportSummary(
            TotalAppointments: appointments.Count,
            CompletedAppointments: appointments.Count(a => a.Status == AppointmentStatus.Completed),
            NewPatients: newPatients,
            TotalBilled: invoices.Sum(i => i.TotalAmount),
            TotalCollected: payments.Sum(p => p.Amount),
            Appointments: appointments,
            Invoices: invoices,
            RevenueByMethod: revenueByMethod,
            TotalDiscount: invoices.Sum(i => i.DiscountAmount),
            RevenueByReceptionist: revenueByReceptionist);
    }

    public async Task<IReadOnlyList<SupplierReportSummary>> GetSupplierReportAsync(
        DateTime from,
        DateTime to,
        string? supplierFilter = null,
        CancellationToken ct = default)
    {
        var details = await GetSupplierBatchDetailsAsync(from, to, supplierFilter, ct);
        var expiryCutoff = DateTime.Today.AddDays(30);

        // Also include returns whose medicine wasn't purchased in this exact window
        var utcStart = from.Date.ToUniversalTime();
        var utcEnd = to.Date.AddDays(1).ToUniversalTime();
        var returns = await _context.StockTransactions
            .Include(t => t.Medicine)
            .Where(t => t.TransactionType == StockTransactionType.Returned &&
                        t.TransactionDate >= utcStart &&
                        t.TransactionDate < utcEnd)
            .ToListAsync(ct);

        var returnBatchIds = returns.Where(r => r.BatchId.HasValue).Select(r => r.BatchId!.Value).Distinct().ToList();
        var returnBatches = await _context.MedicineBatches
            .Where(b => returnBatchIds.Contains(b.BatchId))
            .ToDictionaryAsync(b => b.BatchId, ct);

        var extraReturnsBySupplier = returns
            .GroupBy(r =>
            {
                returnBatches.TryGetValue(r.BatchId ?? 0, out var b);
                return !string.IsNullOrWhiteSpace(b?.Supplier)
                    ? b!.Supplier!.Trim()
                    : (!string.IsNullOrWhiteSpace(r.Medicine?.Company) ? r.Medicine!.Company!.Trim() : "Unspecified");
            }, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Quantity)), StringComparer.OrdinalIgnoreCase);

        var grouped = details
            .GroupBy(d => d.SupplierName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var name = g.First().SupplierName;
                var retUnits = extraReturnsBySupplier.TryGetValue(name, out var r) ? r : g.Sum(x => x.UnitsReturned);
                var expUnits = g.Where(x => x.ExpiryDate.Date <= expiryCutoff).Sum(x => x.RemainingStock);
                return new SupplierReportSummary(
                    SupplierName: name,
                    BatchCount: g.Count(),
                    UnitsPurchased: g.Sum(x => x.UnitsPurchased),
                    PurchaseCost: g.Sum(x => x.TotalCost),
                    UnitsReturned: retUnits,
                    ExpiringOrExpiredUnits: expUnits,
                    RemainingStock: g.Sum(x => x.RemainingStock));
            })
            .ToList();

        // Add any suppliers that only had returns in this period
        foreach (var kvp in extraReturnsBySupplier)
        {
            if (!grouped.Any(g => g.SupplierName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(supplierFilter) &&
                    !kvp.Key.Contains(supplierFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                grouped.Add(new SupplierReportSummary(
                    SupplierName: kvp.Key,
                    BatchCount: 0,
                    UnitsPurchased: 0,
                    PurchaseCost: 0m,
                    UnitsReturned: kvp.Value,
                    ExpiringOrExpiredUnits: 0,
                    RemainingStock: 0));
            }
        }

        return grouped.OrderByDescending(s => s.PurchaseCost).ThenByDescending(s => s.UnitsReturned).ToList();
    }

    public async Task<IReadOnlyList<SupplierBatchDetail>> GetSupplierBatchDetailsAsync(
        DateTime from,
        DateTime to,
        string? supplierFilter = null,
        CancellationToken ct = default)
    {
        var utcStart = from.Date.ToUniversalTime();
        var utcEnd = to.Date.AddDays(1).ToUniversalTime();
        var today = DateTime.Today;
        var expiryCutoff = today.AddDays(30);

        var purchases = await _context.StockTransactions
            .Include(t => t.Medicine)
            .Where(t => t.TransactionType == StockTransactionType.Purchase &&
                        t.TransactionDate >= utcStart &&
                        t.TransactionDate < utcEnd)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(ct);

        var batchIds = purchases.Where(p => p.BatchId.HasValue).Select(p => p.BatchId!.Value).Distinct().ToList();
        var batches = await _context.MedicineBatches
            .Where(b => batchIds.Contains(b.BatchId))
            .ToDictionaryAsync(b => b.BatchId, ct);

        var returnsByBatch = await _context.StockTransactions
            .Where(t => t.TransactionType == StockTransactionType.Returned &&
                        t.BatchId.HasValue &&
                        batchIds.Contains(t.BatchId.Value))
            .GroupBy(t => t.BatchId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => Math.Abs(x.Quantity)), ct);

        var list = purchases.Select(tx =>
        {
            batches.TryGetValue(tx.BatchId ?? 0, out var batch);
            var supplier = !string.IsNullOrWhiteSpace(batch?.Supplier)
                ? batch!.Supplier!.Trim()
                : (!string.IsNullOrWhiteSpace(tx.Medicine?.Company) ? tx.Medicine!.Company!.Trim() : "Unspecified");
            var unitCost = batch?.PurchasePrice > 0 ? batch.PurchasePrice : (tx.Medicine?.PurchasePrice ?? 0m);
            var expiryDate = batch?.ExpiryDate ?? today.AddYears(1);
            var expiryStatus = expiryDate.Date < today
                ? "Expired"
                : (expiryDate.Date <= expiryCutoff ? "Expiring (30d)" : "OK");
            var returnedQty = batch is not null && returnsByBatch.TryGetValue(batch.BatchId, out var rq) ? rq : 0;

            return new SupplierBatchDetail(
                PurchaseDate: tx.TransactionDate.ToLocalTime(),
                SupplierName: supplier,
                MedicineName: tx.Medicine?.Name ?? "Unknown",
                BatchNumber: batch?.BatchNumber ?? "-",
                ExpiryDate: expiryDate,
                ExpiryStatus: expiryStatus,
                UnitsPurchased: tx.Quantity,
                UnitsReturned: returnedQty,
                RemainingStock: batch?.QuantityRemaining ?? 0,
                TotalCost: tx.Quantity * unitCost);
        });

        if (!string.IsNullOrWhiteSpace(supplierFilter))
        {
            var term = supplierFilter.Trim();
            list = list.Where(x =>
                x.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.MedicineName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return list.ToList();
    }
}

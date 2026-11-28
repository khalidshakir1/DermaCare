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

    public async Task<IReadOnlyList<CompanyPurchaseSummary>> GetCompanyPurchasesAsync(
        DateTime from,
        DateTime to,
        string? companyFilter = null,
        bool groupByVendor = false,
        CancellationToken ct = default)
    {
        var utcStart = from.Date.ToUniversalTime();
        var utcEnd = to.Date.AddDays(1).ToUniversalTime();

        var purchases = await _context.StockTransactions
            .Include(t => t.Medicine)
            .Where(t => t.TransactionType == StockTransactionType.Purchase &&
                        t.TransactionDate >= utcStart &&
                        t.TransactionDate < utcEnd)
            .ToListAsync(ct);

        var batchIds = purchases.Where(p => p.BatchId.HasValue).Select(p => p.BatchId!.Value).Distinct().ToList();
        var batches = await _context.MedicineBatches
            .Where(b => batchIds.Contains(b.BatchId))
            .ToDictionaryAsync(b => b.BatchId, ct);

        // Also compute medicine sales in the date range for brand-wise sales reporting
        var soldItems = await _context.InvoiceItems
            .Include(i => i.Invoice)
            .Include(i => i.Medicine)
            .Where(i => i.MedicineId.HasValue &&
                        i.Invoice != null &&
                        i.Invoice.IssuedAt >= utcStart &&
                        i.Invoice.IssuedAt < utcEnd &&
                        i.Invoice.Status != InvoiceStatus.Void)
            .ToListAsync(ct);

        var salesByBrand = soldItems
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Medicine?.Company) ? "Unspecified" : i.Medicine!.Company!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (Units: g.Sum(x => x.Quantity), Revenue: g.Sum(x => x.Quantity * x.UnitPrice - x.DiscountAmount)),
                StringComparer.OrdinalIgnoreCase);

        var rows = purchases.Select(tx =>
        {
            batches.TryGetValue(tx.BatchId ?? 0, out var batch);
            var brand = string.IsNullOrWhiteSpace(tx.Medicine?.Company) ? "Unspecified" : tx.Medicine!.Company!.Trim();
            var vendor = string.IsNullOrWhiteSpace(batch?.Supplier) ? "Unspecified" : batch!.Supplier!.Trim();
            var key = groupByVendor ? vendor : brand;
            var unitCost = batch?.PurchasePrice > 0 ? batch.PurchasePrice : (tx.Medicine?.PurchasePrice ?? 0m);
            return new
            {
                Key = key,
                BatchId = batch?.BatchId ?? tx.TransactionId,
                Units = tx.Quantity,
                Cost = tx.Quantity * unitCost,
                Remaining = batch?.QuantityRemaining ?? 0
            };
        });

        if (!string.IsNullOrWhiteSpace(companyFilter))
        {
            var term = companyFilter.Trim();
            rows = rows.Where(r => r.Key.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return rows
            .GroupBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var name = g.First().Key;
                var sales = !groupByVendor && salesByBrand.TryGetValue(name, out var s) ? s : (Units: 0, Revenue: 0m);
                return new CompanyPurchaseSummary(
                    CompanyName: name,
                    BatchCount: g.Select(x => x.BatchId).Distinct().Count(),
                    TotalUnitsPurchased: g.Sum(x => x.Units),
                    TotalPurchaseCost: g.Sum(x => x.Cost),
                    CurrentUnitsRemaining: g.Sum(x => x.Remaining),
                    UnitsSold: sales.Units,
                    SalesRevenue: sales.Revenue);
            })
            .OrderByDescending(c => c.TotalPurchaseCost)
            .ToList();
    }

    public async Task<IReadOnlyList<BrandVendorPurchaseDetail>> GetBrandVendorPurchaseDetailsAsync(
        DateTime from,
        DateTime to,
        string? filter = null,
        CancellationToken ct = default)
    {
        var utcStart = from.Date.ToUniversalTime();
        var utcEnd = to.Date.AddDays(1).ToUniversalTime();

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

        var list = purchases.Select(tx =>
        {
            batches.TryGetValue(tx.BatchId ?? 0, out var batch);
            var brand = string.IsNullOrWhiteSpace(tx.Medicine?.Company) ? "Unspecified" : tx.Medicine!.Company!.Trim();
            var vendor = string.IsNullOrWhiteSpace(batch?.Supplier) ? "Unspecified" : batch!.Supplier!.Trim();
            var unitCost = batch?.PurchasePrice > 0 ? batch.PurchasePrice : (tx.Medicine?.PurchasePrice ?? 0m);
            return new BrandVendorPurchaseDetail(
                PurchaseDate: tx.TransactionDate.ToLocalTime(),
                MedicineName: tx.Medicine?.Name ?? "Unknown",
                BrandName: brand,
                VendorName: vendor,
                BatchNumber: batch?.BatchNumber ?? "-",
                UnitsPurchased: tx.Quantity,
                UnitCost: unitCost,
                TotalCost: tx.Quantity * unitCost,
                RemainingStock: batch?.QuantityRemaining ?? 0);
        });

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim();
            list = list.Where(x =>
                x.BrandName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.VendorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.MedicineName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return list.ToList();
    }
}

using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class BillingRepository : IBillingRepository
{
    private readonly DermaDbContext _context;

    public BillingRepository(DermaDbContext context) => _context = context;

    public Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken ct = default) =>
        _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, ct);

    public async Task<IReadOnlyList<Invoice>> GetInvoicesByPatientAsync(int patientId, CancellationToken ct = default) =>
        await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetRecentInvoicesAsync(int take = 50, CancellationToken ct = default) =>
        await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.IssuedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceItem>> GetActiveServicesAsync(CancellationToken ct = default) =>
        await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.ServiceName).ToListAsync(ct);

    public async Task<string> GenerateNextBillNumberAsync(CancellationToken ct = default)
    {
        var maxId = await _context.Invoices.Select(i => (int?)i.InvoiceId).MaxAsync(ct) ?? 0;
        return $"BILL-{maxId + 1:D4}";
    }

    public async Task<decimal> GetTodaysRevenueAsync(CancellationToken ct = default)
    {
        var todayUtcStart = DateTime.Today.ToUniversalTime();
        var todayUtcEnd = DateTime.Today.AddDays(1).ToUniversalTime();
        return await _context.Payments
            .Where(p => p.PaidAt >= todayUtcStart && p.PaidAt < todayUtcEnd)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
    }

    public async Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default) =>
        await _context.ClinicProfiles.FirstOrDefaultAsync(ct) ?? new ClinicProfile();

    public async Task AddInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
    }
}

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

        return new ClinicReportSummary(
            TotalAppointments: appointments.Count,
            CompletedAppointments: appointments.Count(a => a.Status == AppointmentStatus.Completed),
            NewPatients: newPatients,
            TotalBilled: invoices.Sum(i => i.TotalAmount),
            TotalCollected: payments.Sum(p => p.Amount),
            Appointments: appointments,
            Invoices: invoices,
            RevenueByMethod: revenueByMethod);
    }
}

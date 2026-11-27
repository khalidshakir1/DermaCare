using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly DermaDbContext _context;

    public AppointmentRepository(DermaDbContext context) => _context = context;

    public Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken ct = default) =>
        _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, ct);

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, int? doctorId = null, CancellationToken ct = default)
    {
        var day = date.Date;
        var q = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Where(a => a.AppointmentDate.Date == day);

        if (doctorId.HasValue)
            q = q.Where(a => a.DoctorId == doctorId.Value);

        return await q.OrderBy(a => a.TokenSequence).ThenBy(a => a.AppointmentTime).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Appointment>> GetTokenHistoryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .AsQueryable();

        if (from.HasValue)
        {
            var start = from.Value.Date;
            q = q.Where(a => a.AppointmentDate >= start);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            q = q.Where(a => a.AppointmentDate < end);
        }

        return await q
            .Where(a => !string.IsNullOrEmpty(a.TokenNumber))
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.TokenSequence)
            .Take(500)
            .ToListAsync(ct);
    }

    public async Task<int> GetNextTokenSequenceAsync(DateTime date, bool isWalkIn, CancellationToken ct = default)
    {
        var day = date.Date;
        var q = _context.Appointments.Where(a => a.AppointmentDate.Date == day);
        q = isWalkIn
            ? q.Where(a => a.Type == AppointmentType.WalkIn)
            : q.Where(a => a.Type != AppointmentType.WalkIn);

        var maxSeq = await q
            .Select(a => (int?)a.TokenSequence)
            .MaxAsync(ct) ?? 0;
        return maxSeq + 1;
    }

    public Task<bool> HasConflictAsync(int doctorId, DateTime date, TimeSpan time, int? excludeAppointmentId = null, CancellationToken ct = default)
    {
        var day = date.Date;
        return _context.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == day &&
            a.AppointmentTime == time &&
            a.Status != AppointmentStatus.Cancelled &&
            (!excludeAppointmentId.HasValue || a.AppointmentId != excludeAppointmentId.Value), ct);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken ct = default)
    {
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
    {
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(ct);
    }
}

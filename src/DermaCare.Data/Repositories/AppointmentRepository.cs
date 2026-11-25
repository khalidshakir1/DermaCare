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

        return await q.OrderBy(a => a.AppointmentTime).ToListAsync(ct);
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

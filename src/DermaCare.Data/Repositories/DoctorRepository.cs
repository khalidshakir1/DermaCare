using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly DermaDbContext _context;

    public DoctorRepository(DermaDbContext context) => _context = context;

    public Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken ct = default) =>
        _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId, ct);

    public async Task<IReadOnlyList<Doctor>> GetAllActiveAsync(CancellationToken ct = default) =>
        await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Schedules)
            .Where(d => d.IsActive)
            .ToListAsync(ct);

    public async Task AddAsync(Doctor doctor, CancellationToken ct = default)
    {
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Doctor doctor, CancellationToken ct = default)
    {
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync(ct);
    }
}

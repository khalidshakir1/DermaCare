using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly DermaDbContext _context;

    public PatientRepository(DermaDbContext context) => _context = context;

    public Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default) =>
        _context.Patients
            .Include(p => p.Appointments)
            .Include(p => p.MedicalRecords)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct);

    public async Task<IReadOnlyList<Patient>> SearchAsync(string? query, bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _context.Patients.AsQueryable();
        if (!includeInactive)
            q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                p.PatientCode.Contains(term) ||
                p.Phone.Contains(term) ||
                (p.CNIC != null && p.CNIC.Contains(term)));
        }

        return await q.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<string> GenerateNextPatientCodeAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Patients.CountAsync(ct) + 1;
        return $"DERM-{year}-{count:D5}";
    }

    public async Task AddAsync(Patient patient, CancellationToken ct = default)
    {
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(ct);
    }
}

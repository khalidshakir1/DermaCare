using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class MedicalRecordRepository : IMedicalRecordRepository
{
    private readonly DermaDbContext _context;

    public MedicalRecordRepository(DermaDbContext context) => _context = context;

    public Task<MedicalRecord?> GetByIdAsync(int recordId, CancellationToken ct = default) =>
        _context.MedicalRecords
            .Include(m => m.Prescriptions)
            .Include(m => m.Photos)
            .Include(m => m.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(m => m.RecordId == recordId, ct);

    public async Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId, CancellationToken ct = default) =>
        await _context.MedicalRecords
            .Include(m => m.Prescriptions)
            .Include(m => m.Doctor).ThenInclude(d => d!.User)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(MedicalRecord record, CancellationToken ct = default)
    {
        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MedicalRecord record, CancellationToken ct = default)
    {
        _context.MedicalRecords.Update(record);
        await _context.SaveChangesAsync(ct);
    }
}

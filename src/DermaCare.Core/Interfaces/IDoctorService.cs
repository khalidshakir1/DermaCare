using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IDoctorService
{
    Task<IReadOnlyList<Doctor>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetDoctorUsersAsync(CancellationToken ct = default);
    Task<ServiceResult<Doctor>> CreateAsync(Doctor doctor, CancellationToken ct = default);
    Task<ServiceResult<Doctor>> UpdateAsync(Doctor doctor, CancellationToken ct = default);
    bool IsWithinSchedule(Doctor doctor, DateTime date, TimeSpan time);
}

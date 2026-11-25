using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record ServiceResult<T>(bool Success, T? Data = default, string? ErrorMessage = null);

public interface IPatientService
{
    Task<IReadOnlyList<Patient>> SearchAsync(string? query, bool includeInactive = false, CancellationToken ct = default);
    Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default);
    Task<ServiceResult<Patient>> CreateAsync(Patient patient, CancellationToken ct = default);
    Task<ServiceResult<Patient>> UpdateAsync(Patient patient, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeactivateAsync(int patientId, CancellationToken ct = default);
}

using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecord?> GetByIdAsync(int recordId, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId, CancellationToken ct = default);
    Task<ServiceResult<MedicalRecord>> SaveConsultationAsync(MedicalRecord record, CancellationToken ct = default);
}

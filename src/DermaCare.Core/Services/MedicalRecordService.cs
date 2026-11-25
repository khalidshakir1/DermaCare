using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IMedicalRecordRepository _recordRepository;

    public MedicalRecordService(IMedicalRecordRepository recordRepository) =>
        _recordRepository = recordRepository;

    public Task<MedicalRecord?> GetByIdAsync(int recordId, CancellationToken ct = default) =>
        _recordRepository.GetByIdAsync(recordId, ct);

    public Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId, CancellationToken ct = default) =>
        _recordRepository.GetByPatientAsync(patientId, ct);

    public async Task<ServiceResult<MedicalRecord>> SaveConsultationAsync(MedicalRecord record, CancellationToken ct = default)
    {
        if (record.PatientId <= 0)
            return new ServiceResult<MedicalRecord>(false, ErrorMessage: "Patient is required.");
        if (record.AppointmentId <= 0)
            return new ServiceResult<MedicalRecord>(false, ErrorMessage: "Appointment is required.");
        if (string.IsNullOrWhiteSpace(record.ChiefComplaint))
            return new ServiceResult<MedicalRecord>(false, ErrorMessage: "Chief complaint is required.");
        if (string.IsNullOrWhiteSpace(record.Diagnosis))
            return new ServiceResult<MedicalRecord>(false, ErrorMessage: "Diagnosis is required.");

        record.ChiefComplaint = record.ChiefComplaint.Trim();
        record.Diagnosis = record.Diagnosis.Trim();
        record.TreatmentPlan = record.TreatmentPlan?.Trim() ?? string.Empty;

        if (record.RecordId == 0)
        {
            record.CreatedAt = DateTime.UtcNow;
            await _recordRepository.AddAsync(record, ct);
        }
        else
        {
            await _recordRepository.UpdateAsync(record, ct);
        }

        return new ServiceResult<MedicalRecord>(true, record);
    }
}

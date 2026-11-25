using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;

    public PatientService(IPatientRepository patientRepository) =>
        _patientRepository = patientRepository;

    public Task<IReadOnlyList<Patient>> SearchAsync(string? query, bool includeInactive = false, CancellationToken ct = default) =>
        _patientRepository.SearchAsync(query, includeInactive, ct);

    public Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default) =>
        _patientRepository.GetByIdAsync(patientId, ct);

    public async Task<ServiceResult<Patient>> CreateAsync(Patient patient, CancellationToken ct = default)
    {
        var error = Validate(patient);
        if (error is not null)
            return new ServiceResult<Patient>(false, ErrorMessage: error);

        patient.FirstName = patient.FirstName.Trim();
        patient.LastName = patient.LastName.Trim();
        patient.Phone = patient.Phone.Trim();
        patient.PatientCode = await _patientRepository.GenerateNextPatientCodeAsync(ct);
        patient.IsActive = true;
        patient.CreatedAt = DateTime.UtcNow;

        await _patientRepository.AddAsync(patient, ct);
        return new ServiceResult<Patient>(true, patient);
    }

    public async Task<ServiceResult<Patient>> UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        var error = Validate(patient);
        if (error is not null)
            return new ServiceResult<Patient>(false, ErrorMessage: error);

        patient.FirstName = patient.FirstName.Trim();
        patient.LastName = patient.LastName.Trim();
        patient.Phone = patient.Phone.Trim();

        await _patientRepository.UpdateAsync(patient, ct);
        return new ServiceResult<Patient>(true, patient);
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(int patientId, CancellationToken ct = default)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId, ct);
        if (patient is null)
            return new ServiceResult<bool>(false, false, "Patient not found.");

        patient.IsActive = false;
        await _patientRepository.UpdateAsync(patient, ct);
        return new ServiceResult<bool>(true, true);
    }

    private static string? Validate(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FirstName))
            return "First name is required.";
        if (string.IsNullOrWhiteSpace(patient.LastName))
            return "Last name is required.";
        if (string.IsNullOrWhiteSpace(patient.Phone))
            return "Phone number is required.";
        if (string.IsNullOrWhiteSpace(patient.Gender))
            return "Gender is required.";
        if (patient.DateOfBirth > DateTime.Today)
            return "Date of birth cannot be in the future.";

        return null;
    }
}

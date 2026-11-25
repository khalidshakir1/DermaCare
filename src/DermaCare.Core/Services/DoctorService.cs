using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUserRepository _userRepository;

    public DoctorService(IDoctorRepository doctorRepository, IUserRepository userRepository)
    {
        _doctorRepository = doctorRepository;
        _userRepository = userRepository;
    }

    public Task<IReadOnlyList<Doctor>> GetAllActiveAsync(CancellationToken ct = default) =>
        _doctorRepository.GetAllActiveAsync(ct);

    public Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken ct = default) =>
        _doctorRepository.GetByIdAsync(doctorId, ct);

    public async Task<IReadOnlyList<User>> GetDoctorUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        return users
            .Where(u => u.IsActive && string.Equals(u.Role?.RoleName, "Doctor", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<ServiceResult<Doctor>> CreateAsync(Doctor doctor, CancellationToken ct = default)
    {
        var error = Validate(doctor);
        if (error is not null)
            return new ServiceResult<Doctor>(false, ErrorMessage: error);

        doctor.Specialization = doctor.Specialization.Trim();
        doctor.Phone = doctor.Phone.Trim();
        doctor.IsActive = true;

        await _doctorRepository.AddAsync(doctor, ct);
        return new ServiceResult<Doctor>(true, doctor);
    }

    public async Task<ServiceResult<Doctor>> UpdateAsync(Doctor doctor, CancellationToken ct = default)
    {
        var error = Validate(doctor);
        if (error is not null)
            return new ServiceResult<Doctor>(false, ErrorMessage: error);

        doctor.Specialization = doctor.Specialization.Trim();
        doctor.Phone = doctor.Phone.Trim();

        await _doctorRepository.UpdateAsync(doctor, ct);
        return new ServiceResult<Doctor>(true, doctor);
    }

    public bool IsWithinSchedule(Doctor doctor, DateTime date, TimeSpan time) =>
        doctor.Schedules.Any(s =>
            s.DayOfWeek == date.DayOfWeek &&
            time >= s.StartTime &&
            time < s.EndTime);

    private static string? Validate(Doctor doctor)
    {
        if (doctor.UserId <= 0)
            return "A linked system user account is required.";
        if (string.IsNullOrWhiteSpace(doctor.Specialization))
            return "Specialization is required.";
        if (doctor.Schedules.Any(s => s.StartTime >= s.EndTime))
            return "Schedule start time must be earlier than end time.";

        return null;
    }
}

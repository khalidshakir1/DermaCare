using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDoctorService _doctorService;
    private readonly ICurrentSession _session;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IDoctorService doctorService,
        ICurrentSession session)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _doctorService = doctorService;
        _session = session;
    }

    public Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, int? doctorId = null, CancellationToken ct = default) =>
        _appointmentRepository.GetByDateAsync(date, doctorId, ct);

    public Task<IReadOnlyList<Appointment>> GetTokenHistoryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default) =>
        _appointmentRepository.GetTokenHistoryAsync(from, to, ct);

    public async Task<ServiceResult<Appointment>> BookAsync(Appointment appointment, CancellationToken ct = default)
    {
        var isWalkIn = appointment.Type == AppointmentType.WalkIn;

        if (!isWalkIn && (!appointment.PatientId.HasValue || appointment.PatientId <= 0) && string.IsNullOrWhiteSpace(appointment.PatientName))
            return new ServiceResult<Appointment>(false, ErrorMessage: "Please enter patient name.");

        if (!isWalkIn && (!appointment.PatientId.HasValue || appointment.PatientId <= 0) && string.IsNullOrWhiteSpace(appointment.PatientPhone))
            return new ServiceResult<Appointment>(false, ErrorMessage: "Please enter patient phone number.");

        Doctor? doctor = null;
        if (appointment.DoctorId > 0)
        {
            doctor = await _doctorRepository.GetByIdAsync(appointment.DoctorId, ct);
        }
        else
        {
            var activeDoctors = await _doctorRepository.GetAllActiveAsync(ct);
            doctor = activeDoctors.FirstOrDefault();
            if (doctor is not null)
                appointment.DoctorId = doctor.DoctorId;
        }

        if (doctor is null)
            return new ServiceResult<Appointment>(false, ErrorMessage: "No clinic doctor configured. Please check Settings.");

        if (!isWalkIn)
        {
            if (doctor.Schedules.Count > 0 &&
                !_doctorService.IsWithinSchedule(doctor, appointment.AppointmentDate, appointment.AppointmentTime))
            {
                return new ServiceResult<Appointment>(false, ErrorMessage: "Selected time is outside the doctor's weekly schedule.");
            }

            var hasConflict = await _appointmentRepository.HasConflictAsync(
                appointment.DoctorId,
                appointment.AppointmentDate,
                appointment.AppointmentTime,
                ct: ct);

            if (hasConflict)
                return new ServiceResult<Appointment>(false, ErrorMessage: "An appointment already exists at that date and time.");
        }

        var nextSeq = await _appointmentRepository.GetNextTokenSequenceAsync(appointment.AppointmentDate, isWalkIn, ct);
        if (nextSeq <= 0) nextSeq = 1;

        var prefix = isWalkIn ? "W" : "P";
        appointment.Status = AppointmentStatus.Scheduled;
        appointment.TokenSequence = nextSeq;
        appointment.TokenNumber = $"{prefix}-{nextSeq:D3}";
        appointment.PatientName = string.IsNullOrWhiteSpace(appointment.PatientName) ? null : appointment.PatientName.Trim();
        appointment.PatientPhone = string.IsNullOrWhiteSpace(appointment.PatientPhone) ? null : appointment.PatientPhone.Trim();
        appointment.CreatedBy = _session.CurrentUser?.UserId ?? 0;
        appointment.CreatedAt = DateTime.UtcNow;

        await _appointmentRepository.AddAsync(appointment, ct);
        return new ServiceResult<Appointment>(true, appointment);
    }

    public async Task<ServiceResult<Appointment>> UpdateStatusAsync(
        int appointmentId,
        AppointmentStatus status,
        string? reason = null,
        CancellationToken ct = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment is null)
            return new ServiceResult<Appointment>(false, ErrorMessage: "Appointment not found.");

        appointment.Status = status;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? reason.Trim()
                : $"{appointment.Notes} | {reason.Trim()}";
        }

        await _appointmentRepository.UpdateAsync(appointment, ct);
        return new ServiceResult<Appointment>(true, appointment);
    }
}

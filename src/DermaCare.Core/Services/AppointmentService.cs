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
        if (appointment.PatientId <= 0)
            return new ServiceResult<Appointment>(false, ErrorMessage: "Please select a patient.");
        if (appointment.DoctorId <= 0)
            return new ServiceResult<Appointment>(false, ErrorMessage: "Please select a doctor.");

        var doctor = await _doctorRepository.GetByIdAsync(appointment.DoctorId, ct);
        if (doctor is null)
            return new ServiceResult<Appointment>(false, ErrorMessage: "Selected doctor was not found.");

        if (doctor.Schedules.Count > 0 &&
            appointment.Type != AppointmentType.WalkIn &&
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
            return new ServiceResult<Appointment>(false, ErrorMessage: "This doctor already has an appointment at that date and time.");

        var nextSeq = await _appointmentRepository.GetNextTokenSequenceAsync(appointment.AppointmentDate, ct);
        if (nextSeq <= 0) nextSeq = 1;

        appointment.Status = AppointmentStatus.Scheduled;
        appointment.TokenSequence = nextSeq;
        appointment.TokenNumber = $"T-{nextSeq:D3}";
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

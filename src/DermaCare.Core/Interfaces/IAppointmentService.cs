using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, int? doctorId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Appointment>> GetTokenHistoryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<ServiceResult<Appointment>> BookAsync(Appointment appointment, CancellationToken ct = default);
    Task<ServiceResult<Appointment>> IssueAppointmentTokenAsync(int appointmentId, CancellationToken ct = default);
    Task<ServiceResult<Appointment>> UpdateStatusAsync(int appointmentId, AppointmentStatus status, string? reason = null, CancellationToken ct = default);
}

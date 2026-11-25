using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record PaymentMethodSummary(PaymentMethod Method, decimal Amount, int Count);

public record ClinicReportSummary(
    int TotalAppointments,
    int CompletedAppointments,
    int NewPatients,
    decimal TotalBilled,
    decimal TotalCollected,
    IReadOnlyList<Appointment> Appointments,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<PaymentMethodSummary> RevenueByMethod);

public interface IReportService
{
    Task<ClinicReportSummary> GenerateSummaryAsync(DateTime from, DateTime to, int? doctorId = null, CancellationToken ct = default);
}

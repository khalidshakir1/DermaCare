using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record PaymentMethodSummary(PaymentMethod Method, decimal Amount, int Count);

public record ReceptionistRevenueSummary(int UserId, string ReceptionistName, decimal TotalCollected, int BillCount, decimal TotalDiscount);

public record CompanyPurchaseSummary(
    string CompanyName,
    int BatchCount,
    int TotalUnitsPurchased,
    decimal TotalPurchaseCost,
    int CurrentUnitsRemaining);

public record ClinicReportSummary(
    int TotalAppointments,
    int CompletedAppointments,
    int NewPatients,
    decimal TotalBilled,
    decimal TotalCollected,
    IReadOnlyList<Appointment> Appointments,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<PaymentMethodSummary> RevenueByMethod,
    decimal TotalDiscount = 0m,
    IReadOnlyList<ReceptionistRevenueSummary>? RevenueByReceptionist = null);

public interface IReportService
{
    Task<ClinicReportSummary> GenerateSummaryAsync(DateTime from, DateTime to, int? doctorId = null, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyPurchaseSummary>> GetCompanyPurchasesAsync(DateTime from, DateTime to, string? companyFilter = null, CancellationToken ct = default);
}

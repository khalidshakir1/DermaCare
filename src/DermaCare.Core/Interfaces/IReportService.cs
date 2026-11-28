using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record PaymentMethodSummary(PaymentMethod Method, decimal Amount, int Count);

public record ReceptionistRevenueSummary(int UserId, string ReceptionistName, decimal TotalCollected, int BillCount, decimal TotalDiscount);

public record CompanyPurchaseSummary(
    string CompanyName,
    int BatchCount,
    int TotalUnitsPurchased,
    decimal TotalPurchaseCost,
    int CurrentUnitsRemaining,
    int UnitsSold = 0,
    decimal SalesRevenue = 0m);

public record BrandVendorPurchaseDetail(
    DateTime PurchaseDate,
    string MedicineName,
    string BrandName,
    string VendorName,
    string BatchNumber,
    int UnitsPurchased,
    decimal UnitCost,
    decimal TotalCost,
    int RemainingStock);

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
    Task<IReadOnlyList<CompanyPurchaseSummary>> GetCompanyPurchasesAsync(DateTime from, DateTime to, string? companyFilter = null, bool groupByVendor = false, CancellationToken ct = default);
    Task<IReadOnlyList<BrandVendorPurchaseDetail>> GetBrandVendorPurchaseDetailsAsync(DateTime from, DateTime to, string? filter = null, CancellationToken ct = default);
}

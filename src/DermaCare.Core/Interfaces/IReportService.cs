using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record PaymentMethodSummary(PaymentMethod Method, decimal Amount, int Count);

public record ReceptionistRevenueSummary(int UserId, string ReceptionistName, decimal TotalCollected, int BillCount, decimal TotalDiscount);

public record SupplierReportSummary(
    string SupplierName,
    int BatchCount,
    int UnitsPurchased,
    decimal PurchaseCost,
    int UnitsReturned,
    int ExpiringOrExpiredUnits,
    int RemainingStock);

public record SupplierBatchDetail(
    DateTime PurchaseDate,
    string SupplierName,
    string MedicineName,
    string BatchNumber,
    DateTime ExpiryDate,
    string ExpiryStatus,
    int UnitsPurchased,
    int UnitsReturned,
    int RemainingStock,
    decimal TotalCost);

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
    Task<IReadOnlyList<SupplierReportSummary>> GetSupplierReportAsync(DateTime from, DateTime to, string? supplierFilter = null, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierBatchDetail>> GetSupplierBatchDetailsAsync(DateTime from, DateTime to, string? supplierFilter = null, CancellationToken ct = default);
}

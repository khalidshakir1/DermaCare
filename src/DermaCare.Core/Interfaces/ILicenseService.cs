namespace DermaCare.Core.Interfaces;

public record LicenseStatus(
    bool IsValid,
    bool IsBlocked,
    bool IsInGracePeriod,
    DateTime? ExpiryDate = null,
    int? GraceDaysRemaining = null,
    string? Message = null);

public interface ILicenseService
{
    Task<LicenseStatus> ValidateAsync(CancellationToken ct = default);
}

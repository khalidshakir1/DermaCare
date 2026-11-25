using DermaCare.Core.Interfaces;

namespace DermaCare.Core.Services;

/// <summary>
/// Phase 1 stub implementation of ILicenseService.
/// Phase 2 will replace this with an HTTP client talking to the Oracle VPS License Server
/// with a silent 14-day offline grace period.
/// </summary>
public class StubLicenseService : ILicenseService
{
    public Task<LicenseStatus> ValidateAsync(CancellationToken ct = default) =>
        Task.FromResult(new LicenseStatus(
            IsValid: true,
            IsBlocked: false,
            IsInGracePeriod: false,
            Message: "Active"));
}

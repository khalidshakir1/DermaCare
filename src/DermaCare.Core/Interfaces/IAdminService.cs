using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IAdminService
{
    Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default);
    Task<ServiceResult<ClinicProfile>> SaveClinicProfileAsync(ClinicProfile profile, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default);
    Task<ServiceResult<User>> CreateUserAsync(string username, string fullName, string password, int roleId, CancellationToken ct = default);
    Task ToggleUserActiveAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetPendingDoctorsAsync(CancellationToken ct = default);
    Task<ServiceResult<User>> ApproveDoctorAsync(int userId, int adminUserId, CancellationToken ct = default);
    Task<ServiceResult<bool>> RejectDoctorAsync(int userId, int adminUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceItem>> GetAllServicesAsync(CancellationToken ct = default);
    Task<ServiceResult<ServiceItem>> AddServiceAsync(string name, decimal price, string category, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAuditLogsAsync(int take = 100, CancellationToken ct = default);
    Task<ServiceResult<string>> BackupDatabaseAsync(string destinationFolder, CancellationToken ct = default);
}


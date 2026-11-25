using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class AdminService : IAdminService
{
    private readonly DermaDbContext _context;

    public AdminService(DermaDbContext context) => _context = context;

    public async Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default) =>
        await _context.ClinicProfiles.FirstOrDefaultAsync(ct) ?? new ClinicProfile();

    public async Task<ServiceResult<ClinicProfile>> SaveClinicProfileAsync(ClinicProfile profile, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profile.ClinicName))
            return new ServiceResult<ClinicProfile>(false, ErrorMessage: "Clinic name is required.");

        if (profile.ClinicProfileId == 0)
            _context.ClinicProfiles.Add(profile);
        else
            _context.ClinicProfiles.Update(profile);

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<ClinicProfile>(true, profile);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default) =>
        await _context.Roles.OrderBy(r => r.RoleName).ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default) =>
        await _context.Users.Include(u => u.Role).Include(u => u.DoctorProfile).OrderBy(u => u.FullName).ToListAsync(ct);

    public async Task<ServiceResult<User>> CreateUserAsync(
        string username,
        string fullName,
        string password,
        int roleId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            return new ServiceResult<User>(false, ErrorMessage: "Username, full name, and password are required.");

        var normalized = username.Trim();
        if (await _context.Users.AnyAsync(u => u.Username == normalized, ct))
            return new ServiceResult<User>(false, ErrorMessage: "Username already exists.");

        var user = new User
        {
            Username = normalized,
            FullName = fullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleId,
            IsActive = true,
            IsPendingApproval = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return new ServiceResult<User>(true, user);
    }

    public async Task ToggleUserActiveAsync(int userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) return;
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetPendingDoctorsAsync(CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Role)
            .Include(u => u.DoctorProfile)
            .Where(u => u.IsPendingApproval)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<ServiceResult<User>> ApproveDoctorAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        var user = await _context.Users.Include(u => u.DoctorProfile).FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
            return new ServiceResult<User>(false, ErrorMessage: "User not found.");

        user.IsPendingApproval = false;
        user.IsActive = true;
        if (user.DoctorProfile is not null)
            user.DoctorProfile.IsActive = true;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = "ApproveDoctor",
            TableName = "Users",
            RecordId = user.UserId,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<User>(true, user);
    }

    public async Task<ServiceResult<bool>> RejectDoctorAsync(int userId, int adminUserId, CancellationToken ct = default)
    {
        var user = await _context.Users.Include(u => u.DoctorProfile).FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
            return new ServiceResult<bool>(false, ErrorMessage: "User not found.");

        user.IsPendingApproval = false;
        user.IsActive = false;
        if (user.DoctorProfile is not null)
            user.DoctorProfile.IsActive = false;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = "RejectDoctor",
            TableName = "Users",
            RecordId = user.UserId,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<bool>(true, true);
    }

    public async Task<IReadOnlyList<ServiceItem>> GetAllServicesAsync(CancellationToken ct = default) =>
        await _context.Services.OrderBy(s => s.Category).ThenBy(s => s.ServiceName).ToListAsync(ct);

    public async Task<ServiceResult<ServiceItem>> AddServiceAsync(
        string name,
        decimal price,
        string category,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || price < 0)
            return new ServiceResult<ServiceItem>(false, ErrorMessage: "Valid service name and price are required.");

        var item = new ServiceItem
        {
            ServiceName = name.Trim(),
            DefaultPrice = price,
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            IsActive = true
        };

        _context.Services.Add(item);
        await _context.SaveChangesAsync(ct);
        return new ServiceResult<ServiceItem>(true, item);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAuditLogsAsync(int take = 100, CancellationToken ct = default) =>
        await _context.AuditLogs.OrderByDescending(a => a.Timestamp).Take(take).ToListAsync(ct);

    public async Task<ServiceResult<string>> BackupDatabaseAsync(string destinationFolder, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return new ServiceResult<string>(false, ErrorMessage: "Select a backup folder.");

        Directory.CreateDirectory(destinationFolder);
        var fileName = $"DermaCareDb_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(destinationFolder, fileName);
        var dbName = _context.Database.GetDbConnection().Database;

        await _context.Database.ExecuteSqlRawAsync(
            "BACKUP DATABASE [{0}] TO DISK = {1} WITH FORMAT, INIT",
            new object[] { dbName, fullPath },
            ct);

        return new ServiceResult<string>(true, fullPath);
    }
}

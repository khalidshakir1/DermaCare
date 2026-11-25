using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public record AuthResult(bool Success, string? ErrorMessage = null, User? User = null);

public record DoctorSignupRequest(
    string FullName,
    string Username,
    string Password,
    string ConfirmPassword,
    string PMDCNumber,
    string Specialization,
    string Phone);

public interface ICurrentSession
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    string RoleName { get; }
    bool IsAdmin { get; }
    bool IsDoctor { get; }
    bool IsReceptionist { get; }
    event Action? SessionChanged;

    void SetUser(User user);
    void Clear();
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<AuthResult> RegisterDoctorAsync(DoctorSignupRequest request, CancellationToken ct = default);
    void Logout();
}

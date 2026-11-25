using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentSession _session;

    public AuthService(IUserRepository userRepository, ICurrentSession session)
    {
        _userRepository = userRepository;
        _session = session;
    }

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Username and password are required.");

        var user = await _userRepository.GetByUsernameAsync(username.Trim(), ct);
        if (user is null)
            return new AuthResult(false, "Invalid username or password.");

        if (user.IsPendingApproval)
            return new AuthResult(false, "Your doctor account is pending administrator approval.");

        if (!user.IsActive)
            return new AuthResult(false, "This account has been deactivated. Contact an administrator.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new AuthResult(false, "Invalid username or password.");

        _session.SetUser(user);
        return new AuthResult(true, User: user);
    }

    public async Task<AuthResult> RegisterDoctorAsync(DoctorSignupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.PMDCNumber) ||
            string.IsNullOrWhiteSpace(request.Phone))
        {
            return new AuthResult(false, "Full name, username, password, PMDC number, and phone are required.");
        }

        if (request.Password.Length < 6)
            return new AuthResult(false, "Password must be at least 6 characters.");

        if (request.Password != request.ConfirmPassword)
            return new AuthResult(false, "Passwords do not match.");

        var normalizedUsername = request.Username.Trim();
        var existing = await _userRepository.GetByUsernameAsync(normalizedUsername, ct);
        if (existing is not null)
            return new AuthResult(false, "Username is already taken.");

        var doctorRole = await _userRepository.GetRoleByNameAsync("Doctor", ct);
        if (doctorRole is null)
            return new AuthResult(false, "Doctor role is not configured in the system.");

        var user = new User
        {
            Username = normalizedUsername,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = doctorRole.RoleId,
            IsActive = false,
            IsPendingApproval = true,
            CreatedAt = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            Specialization = string.IsNullOrWhiteSpace(request.Specialization) ? "Dermatology" : request.Specialization.Trim(),
            Phone = request.Phone.Trim(),
            PMDCNumber = request.PMDCNumber.Trim(),
            IsActive = false
        };

        await _userRepository.AddDoctorUserAsync(user, doctor, ct);
        return new AuthResult(true, User: user);
    }

    public void Logout() => _session.Clear();
}

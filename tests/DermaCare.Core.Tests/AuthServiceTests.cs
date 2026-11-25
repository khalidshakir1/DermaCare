using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class AuthServiceTests
{
    private static User CreateUser(string username = "admin", string password = "Password1!", string role = "Admin", bool isActive = true) =>
        new()
        {
            UserId = 1,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Test User",
            IsActive = isActive,
            Role = new Role { RoleId = 1, RoleName = role }
        };

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsSessionAndReturnsSuccess()
    {
        var user = CreateUser();
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var session = new CurrentSession();
        var sut = new AuthService(repo.Object, session);

        var result = await sut.LoginAsync("admin", "Password1!");

        Assert.True(result.Success);
        Assert.True(session.IsAuthenticated);
        Assert.True(session.IsAdmin);
        Assert.Equal("admin", session.CurrentUser?.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailureAndLeavesSessionEmpty()
    {
        var user = CreateUser();
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var session = new CurrentSession();
        var sut = new AuthService(repo.Object, session);

        var result = await sut.LoginAsync("admin", "WrongPassword");

        Assert.False(result.Success);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsFailure()
    {
        var user = CreateUser(isActive: false);
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var session = new CurrentSession();
        var sut = new AuthService(repo.Object, session);

        var result = await sut.LoginAsync("admin", "Password1!");

        Assert.False(result.Success);
        Assert.Contains("deactivated", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        var repo = new Mock<IUserRepository>();
        var session = new CurrentSession();
        session.SetUser(CreateUser(role: "Receptionist"));
        var sut = new AuthService(repo.Object, session);

        sut.Logout();

        Assert.False(session.IsAuthenticated);
        Assert.False(session.IsReceptionist);
    }

    [Fact]
    public async Task LoginAsync_PendingApprovalDoctor_ReturnsFailure()
    {
        var user = CreateUser(isActive: false);
        user.IsPendingApproval = true;
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("doc1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var session = new CurrentSession();
        var sut = new AuthService(repo.Object, session);

        var result = await sut.LoginAsync("doc1", "Password1!");

        Assert.False(result.Success);
        Assert.Contains("pending", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterDoctorAsync_ValidRequest_CreatesPendingDoctor()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("dr_ayesha", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        repo.Setup(r => r.GetRoleByNameAsync("Doctor", It.IsAny<CancellationToken>())).ReturnsAsync(new Role { RoleId = 2, RoleName = "Doctor" });
        var session = new CurrentSession();
        var sut = new AuthService(repo.Object, session);

        var result = await sut.RegisterDoctorAsync(new DoctorSignupRequest(
            "Dr. Ayesha Khan", "dr_ayesha", "Secret123", "Secret123", "PMDC-12345", "Dermatology", "0300-1234567"));

        Assert.True(result.Success);
        Assert.True(result.User?.IsPendingApproval);
        Assert.False(result.User?.IsActive);
        repo.Verify(r => r.AddDoctorUserAsync(It.IsAny<User>(), It.Is<Doctor>(d => d.PMDCNumber == "PMDC-12345"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

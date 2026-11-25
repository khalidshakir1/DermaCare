using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;

namespace DermaCare.Core.Services;

public class CurrentSession : ICurrentSession
{
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public string RoleName => CurrentUser?.Role?.RoleName ?? string.Empty;
    public bool IsAdmin => string.Equals(RoleName, "Admin", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(RoleName, "Doctor", StringComparison.OrdinalIgnoreCase);
    public bool IsDoctor => IsAdmin;
    public bool IsReceptionist => string.Equals(RoleName, "Receptionist", StringComparison.OrdinalIgnoreCase);

    public event Action? SessionChanged;

    public void SetUser(User user)
    {
        CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        SessionChanged?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        SessionChanged?.Invoke();
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;

namespace DermaCare.UI.ViewModels.Settings;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAdminService _adminService;
    private readonly ICurrentSession _session;

    private ClinicProfile _clinicProfile = new();
    private string _newUsername = string.Empty;
    private string _newFullName = string.Empty;
    private string _newPassword = string.Empty;
    private Role? _selectedRole;
    private User? _selectedUser;
    private User? _selectedPendingDoctor;

    private string _newServiceName = string.Empty;
    private decimal _newServicePrice;
    private string _newServiceCategory = "Consultation";
    private ServiceItem? _selectedService;
    private string _backupFolder = @"C:\DermaCareBackups";
    private string? _statusMessage;

    public SettingsViewModel(IAdminService adminService, ICurrentSession session)
    {
        _adminService = adminService;
        _session = session;

        Roles = new ObservableCollection<Role>();
        Users = new ObservableCollection<User>();
        PendingDoctors = new ObservableCollection<User>();
        Services = new ObservableCollection<ServiceItem>();
        AuditLogs = new ObservableCollection<AuditLog>();

        SaveProfileCommand = new RelayCommand(async () => await SaveProfileAsync());
        CreateUserCommand = new RelayCommand(async () => await CreateUserAsync());
        ToggleUserActiveCommand = new RelayCommand(async () => await ToggleUserActiveAsync(), () => SelectedUser is not null);
        ApproveDoctorCommand = new RelayCommand(async () => await ApproveDoctorAsync(), () => SelectedPendingDoctor is not null);
        RejectDoctorCommand = new RelayCommand(async () => await RejectDoctorAsync(), () => SelectedPendingDoctor is not null);
        AddServiceCommand = new RelayCommand(async () => await AddServiceAsync());
        UpdateServiceCommand = new RelayCommand(async () => await UpdateServiceAsync(), () => SelectedService is not null);
        ToggleServiceActiveCommand = new RelayCommand(async () => await ToggleServiceActiveAsync(), () => SelectedService is not null);
        DeleteServiceCommand = new RelayCommand(async () => await DeleteServiceAsync(), () => SelectedService is not null);
        BackupCommand = new RelayCommand(async () => await BackupAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<Role> Roles { get; }
    public ObservableCollection<User> Users { get; }
    public ObservableCollection<User> PendingDoctors { get; }
    public ObservableCollection<ServiceItem> Services { get; }
    public ObservableCollection<AuditLog> AuditLogs { get; }

    public ClinicProfile ClinicProfile { get => _clinicProfile; set => SetProperty(ref _clinicProfile, value); }
    public string NewUsername { get => _newUsername; set => SetProperty(ref _newUsername, value); }
    public string NewFullName { get => _newFullName; set => SetProperty(ref _newFullName, value); }
    public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
    public Role? SelectedRole { get => _selectedRole; set => SetProperty(ref _selectedRole, value); }
    public User? SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }
    public User? SelectedPendingDoctor { get => _selectedPendingDoctor; set => SetProperty(ref _selectedPendingDoctor, value); }

    public string NewServiceName { get => _newServiceName; set => SetProperty(ref _newServiceName, value); }
    public decimal NewServicePrice { get => _newServicePrice; set => SetProperty(ref _newServicePrice, value); }
    public string NewServiceCategory { get => _newServiceCategory; set => SetProperty(ref _newServiceCategory, value); }
    public ServiceItem? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value) && value is not null)
            {
                NewServiceName = value.ServiceName;
                NewServicePrice = value.DefaultPrice;
                NewServiceCategory = value.Category;
            }
        }
    }
    public string BackupFolder { get => _backupFolder; set => SetProperty(ref _backupFolder, value); }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveProfileCommand { get; }
    public ICommand CreateUserCommand { get; }
    public ICommand ToggleUserActiveCommand { get; }
    public ICommand ApproveDoctorCommand { get; }
    public ICommand RejectDoctorCommand { get; }
    public ICommand AddServiceCommand { get; }
    public ICommand UpdateServiceCommand { get; }
    public ICommand ToggleServiceActiveCommand { get; }
    public ICommand DeleteServiceCommand { get; }
    public ICommand BackupCommand { get; }

    private async Task LoadAsync()
    {
        ClinicProfile = await _adminService.GetClinicProfileAsync();

        Roles.Clear();
        foreach (var r in await _adminService.GetRolesAsync()) Roles.Add(r);
        SelectedRole = Roles.FirstOrDefault();

        await RefreshUsersAsync();
        await RefreshServicesAsync();

        AuditLogs.Clear();
        foreach (var log in await _adminService.GetRecentAuditLogsAsync()) AuditLogs.Add(log);
    }

    private async Task RefreshUsersAsync()
    {
        Users.Clear();
        foreach (var u in await _adminService.GetUsersAsync()) Users.Add(u);

        PendingDoctors.Clear();
        foreach (var p in await _adminService.GetPendingDoctorsAsync()) PendingDoctors.Add(p);
    }

    private async Task RefreshServicesAsync()
    {
        Services.Clear();
        foreach (var s in await _adminService.GetAllServicesAsync()) Services.Add(s);
    }

    private async Task SaveProfileAsync()
    {
        var result = await _adminService.SaveClinicProfileAsync(ClinicProfile);
        StatusMessage = result.Success ? "Clinic profile saved." : result.ErrorMessage;
    }

    private async Task CreateUserAsync()
    {
        if (SelectedRole is null) return;
        var result = await _adminService.CreateUserAsync(NewUsername, NewFullName, NewPassword, SelectedRole.RoleId);
        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        NewUsername = string.Empty;
        NewFullName = string.Empty;
        NewPassword = string.Empty;
        StatusMessage = "User created.";
        await RefreshUsersAsync();
    }

    private async Task ToggleUserActiveAsync()
    {
        if (SelectedUser is null) return;
        await _adminService.ToggleUserActiveAsync(SelectedUser.UserId);
        await RefreshUsersAsync();
    }

    private async Task ApproveDoctorAsync()
    {
        if (SelectedPendingDoctor is null) return;
        var adminId = _session.CurrentUser?.UserId ?? 0;
        var result = await _adminService.ApproveDoctorAsync(SelectedPendingDoctor.UserId, adminId);
        StatusMessage = result.Success ? $"Approved Dr. {SelectedPendingDoctor.FullName}." : result.ErrorMessage;
        await RefreshUsersAsync();
    }

    private async Task RejectDoctorAsync()
    {
        if (SelectedPendingDoctor is null) return;
        var adminId = _session.CurrentUser?.UserId ?? 0;
        var result = await _adminService.RejectDoctorAsync(SelectedPendingDoctor.UserId, adminId);
        StatusMessage = result.Success ? $"Rejected registration for {SelectedPendingDoctor.FullName}." : result.ErrorMessage;
        await RefreshUsersAsync();
    }

    private async Task AddServiceAsync()
    {
        var result = await _adminService.AddServiceAsync(NewServiceName, NewServicePrice, NewServiceCategory);
        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        NewServiceName = string.Empty;
        NewServicePrice = 0;
        StatusMessage = "Service added.";
        await RefreshServicesAsync();
    }

    private async Task UpdateServiceAsync()
    {
        if (SelectedService is null) return;
        SelectedService.ServiceName = NewServiceName;
        SelectedService.DefaultPrice = NewServicePrice;
        SelectedService.Category = NewServiceCategory;
        var result = await _adminService.UpdateServiceAsync(SelectedService);
        StatusMessage = result.Success ? $"Updated '{result.Data?.ServiceName}'." : result.ErrorMessage;
        await RefreshServicesAsync();
    }

    private async Task ToggleServiceActiveAsync()
    {
        if (SelectedService is null) return;
        await _adminService.ToggleServiceActiveAsync(SelectedService.ServiceId);
        StatusMessage = $"Toggled status for '{SelectedService.ServiceName}'.";
        await RefreshServicesAsync();
    }

    private async Task DeleteServiceAsync()
    {
        if (SelectedService is null) return;
        var result = await _adminService.DeleteServiceAsync(SelectedService.ServiceId);
        StatusMessage = result.Success ? "Service removed / deactivated." : result.ErrorMessage;
        SelectedService = null;
        NewServiceName = string.Empty;
        NewServicePrice = 0;
        await RefreshServicesAsync();
    }

    private async Task BackupAsync()
    {
        try
        {
            var result = await _adminService.BackupDatabaseAsync(BackupFolder);
            StatusMessage = result.Success ? $"Backup saved: {result.Data}" : result.ErrorMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
    }
}

using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private bool _isRegisterMode;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _fullName = string.Empty;
    private string _pmdcNumber = string.Empty;
    private string _specialization = "Dermatology";
    private string _phone = string.Empty;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isBusy;

    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        LoginCommand = new RelayCommand(async () => await LoginAsync(), () => !IsBusy);
        RegisterDoctorCommand = new RelayCommand(async () => await RegisterDoctorAsync(), () => !IsBusy);
        ToggleModeCommand = new RelayCommand(() =>
        {
            IsRegisterMode = !IsRegisterMode;
            ErrorMessage = null;
            SuccessMessage = null;
        });
    }

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        set
        {
            if (SetProperty(ref _isRegisterMode, value))
                OnPropertyChanged(nameof(IsLoginMode));
        }
    }

    public bool IsLoginMode => !IsRegisterMode;

    public string Username
    {
        get => _username;
        set { if (SetProperty(ref _username, value)) ErrorMessage = null; }
    }

    public string Password
    {
        get => _password;
        set { if (SetProperty(ref _password, value)) ErrorMessage = null; }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { if (SetProperty(ref _confirmPassword, value)) ErrorMessage = null; }
    }

    public string FullName
    {
        get => _fullName;
        set { if (SetProperty(ref _fullName, value)) ErrorMessage = null; }
    }

    public string PMDCNumber
    {
        get => _pmdcNumber;
        set { if (SetProperty(ref _pmdcNumber, value)) ErrorMessage = null; }
    }

    public string Specialization
    {
        get => _specialization;
        set { if (SetProperty(ref _specialization, value)) ErrorMessage = null; }
    }

    public string Phone
    {
        get => _phone;
        set { if (SetProperty(ref _phone, value)) ErrorMessage = null; }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
                OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterDoctorCommand { get; }
    public ICommand ToggleModeCommand { get; }

    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                return;
            }

            Password = string.Empty;
            _navigationService.NavigateTo<DashboardViewModel>();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RegisterDoctorAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await _authService.RegisterDoctorAsync(new DoctorSignupRequest(
                FullName,
                Username,
                Password,
                ConfirmPassword,
                PMDCNumber,
                Specialization,
                Phone));

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Registration failed.";
                return;
            }

            FullName = string.Empty;
            PMDCNumber = string.Empty;
            Phone = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            IsRegisterMode = false;
            SuccessMessage = "Registration submitted! An administrator must approve your account before you can sign in.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

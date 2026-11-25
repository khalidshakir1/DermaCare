using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels.Appointments;
using DermaCare.UI.ViewModels.Auth;
using DermaCare.UI.ViewModels.Billing;
using DermaCare.UI.ViewModels.Doctors;
using DermaCare.UI.ViewModels.Patients;
using DermaCare.UI.ViewModels.Reports;
using DermaCare.UI.ViewModels.Inventory;
using DermaCare.UI.ViewModels.Settings;
using MaterialDesignThemes.Wpf;

namespace DermaCare.UI.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ICurrentSession _session;
    private readonly IAuthService _authService;
    private string _pageTitle = "Sign In";
    private bool _isDarkTheme;

    public MainViewModel(
        INavigationService navigationService,
        ICurrentSession session,
        IAuthService authService)
    {
        _navigationService = navigationService;
        _session = session;
        _authService = authService;

        _navigationService.CurrentViewChanged += () => OnPropertyChanged(nameof(CurrentView));
        _session.SessionChanged += OnSessionChanged;

        NavigateInventoryCommand = new RelayCommand(() => { PageTitle = "Medicine Inventory"; _navigationService.NavigateTo<InventoryViewModel>(); });

        NavigateDashboardCommand = new RelayCommand(() =>
        {
            PageTitle = "Dashboard";
            _navigationService.NavigateTo<DashboardViewModel>();
        });

        NavigatePatientsCommand = new RelayCommand(() =>
        {
            PageTitle = "Patients";
            _navigationService.NavigateTo<PatientListViewModel>();
        });

        NavigateDoctorsCommand = new RelayCommand(() =>
        {
            PageTitle = "Doctors";
            _navigationService.NavigateTo<DoctorListViewModel>();
        });

        NavigateAppointmentsCommand = new RelayCommand(() =>
        {
            PageTitle = "Appointments";
            _navigationService.NavigateTo<AppointmentCalendarViewModel>();
        });

        NavigateBillingCommand = new RelayCommand(() =>
        {
            PageTitle = "Billing & Invoices";
            _navigationService.NavigateTo<InvoiceViewModel>();
        });

        NavigateReportsCommand = new RelayCommand(() =>
        {
            PageTitle = "Reports & Analytics";
            _navigationService.NavigateTo<ReportsViewModel>();
        });

        NavigateSaleSummaryCommand = new RelayCommand(() =>
        {
            PageTitle = "Sale Summary";
            _navigationService.NavigateTo<SaleSummaryViewModel>();
        });

        NavigateSettingsCommand = new RelayCommand(() =>
        {
            PageTitle = "Settings & Administration";
            _navigationService.NavigateTo<SettingsViewModel>();
        });

        QuickNewPatientCommand = new RelayCommand(() =>
        {
            if (!IsAuthenticated) return;
            PageTitle = "New Patient";
            _navigationService.NavigateTo<PatientFormViewModel>(vm => vm.Initialize(null));
        });

        QuickBookAppointmentCommand = new RelayCommand(() =>
        {
            if (!IsAuthenticated) return;
            PageTitle = "Book Appointment";
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(false, DateTime.Today));
        });

        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        LogoutCommand = new RelayCommand(Logout);

        _navigationService.NavigateTo<LoginViewModel>();
    }

    public BaseViewModel? CurrentView => _navigationService.CurrentView;

    public string PageTitle
    {
        get => _pageTitle;
        set => SetProperty(ref _pageTitle, value);
    }

    public bool IsAuthenticated => _session.IsAuthenticated;
    public bool IsAdmin => _session.IsAdmin;
    public bool IsDoctor => _session.IsDoctor;
    public bool IsReceptionist => _session.IsReceptionist;
    public bool CanAccessDoctors => _session.IsAdmin;
    public bool CanAccessReports => _session.IsAdmin;
    public bool CanAccessSaleSummary => _session.IsAdmin;
    public bool CanAccessSettings => _session.IsAdmin;
    public bool CanAccessBilling => _session.IsAuthenticated;
    public bool CanAccessInventory => _session.IsAuthenticated;
    public string RoleBadgeText => _session.IsAdmin ? "ADMIN / DR" : "RECEPTIONIST";
    public string RoleBadgeColor => _session.IsAdmin ? "#1565C0" : "#2E7D32";
    public SnackbarMessageQueue ToastQueue { get; } = new(TimeSpan.FromSeconds(3));
    public string CurrentUserDisplay => _session.CurrentUser is null
        ? string.Empty
        : _session.CurrentUser.FullName;

    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigatePatientsCommand { get; }
    public ICommand NavigateDoctorsCommand { get; }
    public ICommand NavigateAppointmentsCommand { get; }
    public ICommand NavigateBillingCommand { get; }
    public ICommand NavigateSaleSummaryCommand { get; }
    public ICommand NavigateReportsCommand { get; }
    public ICommand NavigateInventoryCommand { get; }
    public ICommand NavigateSettingsCommand { get; }
    public ICommand QuickNewPatientCommand { get; }
    public ICommand QuickBookAppointmentCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LogoutCommand { get; }

    private void OnSessionChanged()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsDoctor));
        OnPropertyChanged(nameof(IsReceptionist));
        OnPropertyChanged(nameof(CanAccessDoctors));
        OnPropertyChanged(nameof(CanAccessReports));
        OnPropertyChanged(nameof(CanAccessSaleSummary));
        OnPropertyChanged(nameof(CanAccessSettings));
        OnPropertyChanged(nameof(CanAccessBilling));
        OnPropertyChanged(nameof(CanAccessInventory));
        OnPropertyChanged(nameof(RoleBadgeText));
        OnPropertyChanged(nameof(RoleBadgeColor));
        OnPropertyChanged(nameof(CurrentUserDisplay));

        if (_session.IsAuthenticated)
        {
            PageTitle = "Dashboard";
            ToastQueue.Enqueue($"Welcome back, {_session.CurrentUser?.FullName}!");
        }
    }

    private void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(_isDarkTheme ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }

    private void Logout()
    {
        _authService.Logout();
        PageTitle = "Sign In";
        _navigationService.NavigateTo<LoginViewModel>();
    }
}



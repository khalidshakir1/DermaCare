using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels.Billing;
using DermaCare.UI.ViewModels.MedicalRecords;

namespace DermaCare.UI.ViewModels.Appointments;

public class AppointmentCalendarViewModel : BaseViewModel
{
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly IBillingService _billingService;
    private readonly INavigationService _navigationService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private DateTime _selectedDate = DateTime.Today;
    private Appointment? _selectedAppointment;
    private string _cancellationReason = string.Empty;
    private DateTime _historyFromDate = DateTime.Today.AddDays(-7);
    private DateTime _historyToDate = DateTime.Today;
    private int _totalTokensIssued;
    private int _completedTokensCount;

    public AppointmentCalendarViewModel(
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        IBillingService billingService,
        INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _billingService = billingService;
        _navigationService = navigationService;

        Appointments = new ObservableCollection<Appointment>();
        TokenHistory = new ObservableCollection<Appointment>();

        RefreshCommand = new RelayCommand(async () => await LoadAppointmentsAsync());
        FilterTokenHistoryCommand = new RelayCommand(async () => await LoadTokenHistoryAsync());
        PrintTokenCommand = new RelayCommand(async () => await PrintSelectedTokenAsync(), () => SelectedAppointment is not null);
        NewAppointmentCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(isWalkIn: false, SelectedDate)));
        WalkInCommand = new RelayCommand(async () => await IssueQuickWalkInTokenAsync());
        ConfirmCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Confirmed),
            () => SelectedAppointment is { Status: AppointmentStatus.Scheduled });
        StartVisitCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.InProgress),
            () => SelectedAppointment is { Status: AppointmentStatus.Scheduled or AppointmentStatus.Confirmed });
        CompleteCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Completed),
            () => SelectedAppointment is { Status: AppointmentStatus.InProgress });
        OpenConsultationCommand = new RelayCommand(
            () => _navigationService.NavigateTo<ConsultationViewModel>(vm => vm.Initialize(SelectedAppointment!)),
            () => SelectedAppointment is { Status: AppointmentStatus.InProgress or AppointmentStatus.Completed });
        GenerateBillCommand = new RelayCommand(
            () => _navigationService.NavigateTo<InvoiceViewModel>(vm => vm.PreselectCustomer(SelectedAppointment!.DisplayPatientName, SelectedAppointment.PatientId)),
            () => SelectedAppointment is not null);
        CancelAppointmentCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Cancelled, CancellationReason),
            () => SelectedAppointment is not null && SelectedAppointment.Status != AppointmentStatus.Cancelled);

        _ = InitializeAsync();
    }

    public ObservableCollection<Appointment> Appointments { get; }
    public ObservableCollection<Appointment> TokenHistory { get; }

    public DateTime HistoryFromDate { get => _historyFromDate; set => SetProperty(ref _historyFromDate, value); }
    public DateTime HistoryToDate { get => _historyToDate; set => SetProperty(ref _historyToDate, value); }
    public int TotalTokensIssued { get => _totalTokensIssued; private set => SetProperty(ref _totalTokensIssued, value); }
    public int CompletedTokensCount { get => _completedTokensCount; private set => SetProperty(ref _completedTokensCount, value); }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
                _ = LoadAppointmentsAsync();
        }
    }

    public Appointment? SelectedAppointment
    {
        get => _selectedAppointment;
        set => SetProperty(ref _selectedAppointment, value);
    }

    public string CancellationReason
    {
        get => _cancellationReason;
        set => SetProperty(ref _cancellationReason, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand FilterTokenHistoryCommand { get; }
    public ICommand PrintTokenCommand { get; }
    public ICommand NewAppointmentCommand { get; }
    public ICommand WalkInCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand StartVisitCommand { get; }
    public ICommand CompleteCommand { get; }
    public ICommand OpenConsultationCommand { get; }
    public ICommand GenerateBillCommand { get; }
    public ICommand CancelAppointmentCommand { get; }

    private async Task InitializeAsync()
    {
        await LoadAppointmentsAsync();
        await LoadTokenHistoryAsync();
    }

    public async Task LoadAppointmentsAsync()
    {
        if (!await _loadLock.WaitAsync(0)) return;
        try
        {
            var items = await _appointmentService.GetByDateAsync(SelectedDate);
            Appointments.Clear();
            foreach (var a in items)
                Appointments.Add(a);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task IssueQuickWalkInTokenAsync()
    {
        var doctors = await _doctorService.GetAllActiveAsync();
        var doc = doctors.FirstOrDefault();
        var walkIn = new Appointment
        {
            DoctorId = doc?.DoctorId ?? 0,
            AppointmentDate = DateTime.Today,
            AppointmentTime = DateTime.Now.TimeOfDay,
            Type = AppointmentType.WalkIn
        };

        var result = await _appointmentService.BookAsync(walkIn);
        if (result.Success && result.Data is not null)
        {
            result.Data.Doctor = doc;
            var clinic = await _billingService.GetClinicProfileAsync();
            ReceiptPrintHelper.PrintThermalToken(result.Data, clinic);
            SelectedDate = DateTime.Today;
            await LoadAppointmentsAsync();
            await LoadTokenHistoryAsync();
        }
    }

    public async Task LoadTokenHistoryAsync()
    {
        var history = await _appointmentService.GetTokenHistoryAsync(HistoryFromDate, HistoryToDate);
        TokenHistory.Clear();
        foreach (var item in history)
            TokenHistory.Add(item);
        TotalTokensIssued = TokenHistory.Count;
        CompletedTokensCount = TokenHistory.Count(a => a.Status == AppointmentStatus.Completed);
    }

    private async Task PrintSelectedTokenAsync()
    {
        if (SelectedAppointment is null) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        ReceiptPrintHelper.PrintThermalToken(SelectedAppointment, clinic);
    }

    private async Task ChangeStatusAsync(AppointmentStatus status, string? reason = null)
    {
        if (SelectedAppointment is null) return;
        await _appointmentService.UpdateStatusAsync(SelectedAppointment.AppointmentId, status, reason);
        CancellationReason = string.Empty;
        await LoadAppointmentsAsync();
    }
}

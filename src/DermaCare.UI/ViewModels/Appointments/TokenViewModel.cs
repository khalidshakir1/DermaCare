using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Appointments;

public class TokenViewModel : BaseViewModel
{
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly IBillingService _billingService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private bool _isWalkInMode = true;
    private Appointment? _selectedBookedAppointment;
    private Appointment? _selectedIssuedToken;
    private DateTime _historyFromDate = DateTime.Today.AddDays(-7);
    private DateTime _historyToDate = DateTime.Today;
    private int _totalTokensIssued;
    private int _completedTokensCount;
    private string? _statusMessage;

    public TokenViewModel(
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        IBillingService billingService)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _billingService = billingService;

        PendingAppointments = new ObservableCollection<Appointment>();
        TodaysTokens = new ObservableCollection<Appointment>();
        TokenHistory = new ObservableCollection<Appointment>();

        IssueWalkInTokenCommand = new RelayCommand(async () => await IssueWalkInTokenAsync());
        IssueAppointmentTokenCommand = new RelayCommand(
            async () => await IssueAppointmentTokenAsync(),
            () => SelectedBookedAppointment is not null);
        ReprintTokenCommand = new RelayCommand(
            async () => await ReprintSelectedTokenAsync(),
            () => SelectedIssuedToken is not null);
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        FilterTokenHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<Appointment> PendingAppointments { get; }
    public ObservableCollection<Appointment> TodaysTokens { get; }
    public ObservableCollection<Appointment> TokenHistory { get; }

    public bool IsWalkInMode
    {
        get => _isWalkInMode;
        set
        {
            if (SetProperty(ref _isWalkInMode, value))
                OnPropertyChanged(nameof(IsAppointmentMode));
        }
    }

    public bool IsAppointmentMode
    {
        get => !_isWalkInMode;
        set
        {
            if (value) IsWalkInMode = false;
        }
    }

    public Appointment? SelectedBookedAppointment
    {
        get => _selectedBookedAppointment;
        set => SetProperty(ref _selectedBookedAppointment, value);
    }

    public Appointment? SelectedIssuedToken
    {
        get => _selectedIssuedToken;
        set => SetProperty(ref _selectedIssuedToken, value);
    }

    public DateTime HistoryFromDate { get => _historyFromDate; set => SetProperty(ref _historyFromDate, value); }
    public DateTime HistoryToDate { get => _historyToDate; set => SetProperty(ref _historyToDate, value); }
    public int TotalTokensIssued { get => _totalTokensIssued; private set => SetProperty(ref _totalTokensIssued, value); }
    public int CompletedTokensCount { get => _completedTokensCount; private set => SetProperty(ref _completedTokensCount, value); }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand IssueWalkInTokenCommand { get; }
    public ICommand IssueAppointmentTokenCommand { get; }
    public ICommand ReprintTokenCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FilterTokenHistoryCommand { get; }

    private async Task LoadAsync()
    {
        if (!await _loadLock.WaitAsync(0)) return;
        try
        {
            var today = await _appointmentService.GetByDateAsync(DateTime.Today);

            PendingAppointments.Clear();
            foreach (var a in today.Where(a =>
                         string.IsNullOrWhiteSpace(a.TokenNumber) &&
                         a.Status != AppointmentStatus.Cancelled &&
                         a.Type != AppointmentType.WalkIn))
            {
                PendingAppointments.Add(a);
            }
            SelectedBookedAppointment = PendingAppointments.FirstOrDefault();

            TodaysTokens.Clear();
            foreach (var a in today.Where(a => !string.IsNullOrWhiteSpace(a.TokenNumber)))
            {
                TodaysTokens.Add(a);
            }

            await LoadHistoryInternalAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadHistoryAsync()
    {
        if (!await _loadLock.WaitAsync(0)) return;
        try
        {
            await LoadHistoryInternalAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadHistoryInternalAsync()
    {
        var history = await _appointmentService.GetTokenHistoryAsync(HistoryFromDate, HistoryToDate);
        TokenHistory.Clear();
        foreach (var item in history)
            TokenHistory.Add(item);
        TotalTokensIssued = TokenHistory.Count;
        CompletedTokensCount = TokenHistory.Count(a => a.Status == AppointmentStatus.Completed);
    }

    private async Task IssueWalkInTokenAsync()
    {
        StatusMessage = null;
        var docs = await _doctorService.GetAllActiveAsync();
        var doc = docs.FirstOrDefault();
        var walkIn = new Appointment
        {
            DoctorId = doc?.DoctorId ?? 0,
            AppointmentDate = DateTime.Today,
            AppointmentTime = DateTime.Now.TimeOfDay,
            Type = AppointmentType.WalkIn
        };

        var result = await _appointmentService.BookAsync(walkIn);
        if (!result.Success || result.Data is null)
        {
            StatusMessage = result.ErrorMessage ?? "Could not issue walk-in token.";
            return;
        }

        result.Data.Doctor = doc;
        var clinic = await _billingService.GetClinicProfileAsync();
        ReceiptPrintHelper.PrintThermalToken(result.Data, clinic);
        StatusMessage = $"Issued Walk-In Token {result.Data.TokenNumber}.";
        await LoadAsync();
    }

    private async Task IssueAppointmentTokenAsync()
    {
        if (SelectedBookedAppointment is null) return;
        StatusMessage = null;

        var result = await _appointmentService.IssueAppointmentTokenAsync(SelectedBookedAppointment.AppointmentId);
        if (!result.Success || result.Data is null)
        {
            StatusMessage = result.ErrorMessage ?? "Could not issue appointment token.";
            return;
        }

        var clinic = await _billingService.GetClinicProfileAsync();
        ReceiptPrintHelper.PrintThermalToken(result.Data, clinic);
        StatusMessage = $"Issued Appointment Token {result.Data.TokenNumber} for {result.Data.DisplayPatientName}.";
        await LoadAsync();
    }

    private async Task ReprintSelectedTokenAsync()
    {
        if (SelectedIssuedToken is null) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        ReceiptPrintHelper.PrintThermalToken(SelectedIssuedToken, clinic);
    }
}

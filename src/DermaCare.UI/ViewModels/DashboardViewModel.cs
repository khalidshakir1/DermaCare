using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels.Appointments;
using DermaCare.UI.ViewModels.Billing;
using DermaCare.UI.ViewModels.Inventory;
using DermaCare.UI.ViewModels.Reports;

namespace DermaCare.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ICurrentSession _session;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly IInventoryService _inventoryService;
    private readonly IBillingService _billingService;
    private readonly IReportService _reportService;
    private readonly INavigationService _navigationService;

    private int _todaysAppointmentCount;
    private int _lowStockCount;
    private int _expiringSoonCount;
    private decimal _todaysRevenue;
    private decimal _todaysDiscounts;
    private decimal _myTodaysRevenue;
    private int _myTodaysBillCount;
    private int _pendingQueueCount;
    private Appointment? _currentToken;
    private Appointment? _nextWaitingToken;

    public DashboardViewModel(
        ICurrentSession session,
        IAppointmentRepository appointmentRepository,
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        IInventoryService inventoryService,
        IBillingService billingService,
        IReportService reportService,
        INavigationService navigationService)
    {
        _session = session;
        _appointmentRepository = appointmentRepository;
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _inventoryService = inventoryService;
        _billingService = billingService;
        _reportService = reportService;
        _navigationService = navigationService;

        TodaysQueue = new ObservableCollection<Appointment>();
        ReceptionistSummaries = new ObservableCollection<ReceptionistRevenueSummary>();

        RefreshCommand = new RelayCommand(async () => await LoadSummaryAsync());
        OpenSaleSummaryCommand = new RelayCommand(() => _navigationService.NavigateTo<SaleSummaryViewModel>());
        OpenPosCommand = new RelayCommand(() => _navigationService.NavigateTo<InvoiceViewModel>());
        OpenBookTokenCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(false, DateTime.Today)));
        IssueWalkInTokenCommand = new RelayCommand(async () => await IssueWalkInTokenAsync());
        CallNextTokenCommand = new RelayCommand(async () => await CallNextTokenAsync());
        OpenInventoryCommand = new RelayCommand(() => _navigationService.NavigateTo<InventoryViewModel>());
        PrintTokenCommand = new RelayCommand(async p =>
        {
            if (p is Appointment appt)
            {
                var clinic = await _billingService.GetClinicProfileAsync();
                ReceiptPrintHelper.PrintThermalToken(appt, clinic);
            }
        });
        CallTokenCommand = new RelayCommand(async p =>
        {
            if (p is Appointment appt)
            {
                await _appointmentService.UpdateStatusAsync(appt.AppointmentId, AppointmentStatus.InProgress);
                await LoadSummaryAsync();
            }
        });
        CompleteTokenCommand = new RelayCommand(async p =>
        {
            if (p is Appointment appt)
            {
                await _appointmentService.UpdateStatusAsync(appt.AppointmentId, AppointmentStatus.Completed);
                await LoadSummaryAsync();
            }
        });

        _ = LoadSummaryAsync();
    }

    public ObservableCollection<Appointment> TodaysQueue { get; }
    public ObservableCollection<ReceptionistRevenueSummary> ReceptionistSummaries { get; }

    public bool IsAdmin => _session.IsAdmin;
    public bool IsReceptionist => _session.IsReceptionist;

    public string WelcomeTitle => _session.CurrentUser is null
        ? "Welcome to DermaCare Clinic"
        : $"Welcome, {_session.CurrentUser.FullName}";

    public string Subtitle => _session.IsAdmin
        ? "Doctor / Admin Control Center — Full Clinic Overview"
        : "Receptionist Front Desk — Appointments, Tokens & POS";

    public int TodaysAppointmentCount { get => _todaysAppointmentCount; private set => SetProperty(ref _todaysAppointmentCount, value); }
    public int LowStockCount { get => _lowStockCount; private set => SetProperty(ref _lowStockCount, value); }
    public int ExpiringSoonCount { get => _expiringSoonCount; private set => SetProperty(ref _expiringSoonCount, value); }
    public decimal TodaysRevenue { get => _todaysRevenue; private set => SetProperty(ref _todaysRevenue, value); }
    public decimal TodaysDiscounts { get => _todaysDiscounts; private set => SetProperty(ref _todaysDiscounts, value); }
    public decimal MyTodaysRevenue { get => _myTodaysRevenue; private set => SetProperty(ref _myTodaysRevenue, value); }
    public int MyTodaysBillCount { get => _myTodaysBillCount; private set => SetProperty(ref _myTodaysBillCount, value); }
    public int PendingQueueCount { get => _pendingQueueCount; private set => SetProperty(ref _pendingQueueCount, value); }

    public string CurrentTokenNumber => _currentToken?.TokenNumber ?? "---";
    public string CurrentTokenPatient => _currentToken is not null
        ? $"{_currentToken.DisplayPatientName} ({_currentToken.Type})"
        : "No patient currently in room";
    public string NextWaitingText => _nextWaitingToken is not null
        ? $"Next Waiting: {_nextWaitingToken.TokenNumber} — {_nextWaitingToken.DisplayPatientName}"
        : "No waiting tokens in queue";

    public ICommand RefreshCommand { get; }
    public ICommand OpenSaleSummaryCommand { get; }
    public ICommand OpenPosCommand { get; }
    public ICommand OpenBookTokenCommand { get; }
    public ICommand IssueWalkInTokenCommand { get; }
    public ICommand CallNextTokenCommand { get; }
    public ICommand OpenInventoryCommand { get; }
    public ICommand PrintTokenCommand { get; }
    public ICommand CallTokenCommand { get; }
    public ICommand CompleteTokenCommand { get; }

    private async Task IssueWalkInTokenAsync()
    {
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
        if (result.Success && result.Data is not null)
        {
            result.Data.Doctor = doc;
            var clinic = await _billingService.GetClinicProfileAsync();
            ReceiptPrintHelper.PrintThermalToken(result.Data, clinic);
            await LoadSummaryAsync();
        }
    }

    private async Task CallNextTokenAsync()
    {
        if (_currentToken is not null)
            await _appointmentService.UpdateStatusAsync(_currentToken.AppointmentId, AppointmentStatus.Completed);

        if (_nextWaitingToken is not null)
            await _appointmentService.UpdateStatusAsync(_nextWaitingToken.AppointmentId, AppointmentStatus.InProgress);

        await LoadSummaryAsync();
    }

    private async Task LoadSummaryAsync()
    {
        var today = await _appointmentRepository.GetByDateAsync(DateTime.Today);

        TodaysQueue.Clear();
        foreach (var a in today.Where(a => !string.IsNullOrWhiteSpace(a.TokenNumber) && a.Status != AppointmentStatus.Cancelled))
            TodaysQueue.Add(a);

        TodaysAppointmentCount = TodaysQueue.Count;
        PendingQueueCount = TodaysQueue.Count(a => a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.InProgress);

        _currentToken = TodaysQueue.FirstOrDefault(a => a.Status == AppointmentStatus.InProgress);
        _nextWaitingToken = TodaysQueue.FirstOrDefault(a => a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed);
        OnPropertyChanged(nameof(CurrentTokenNumber));
        OnPropertyChanged(nameof(CurrentTokenPatient));
        OnPropertyChanged(nameof(NextWaitingText));

        var lowStock = await _inventoryService.GetLowStockMedicinesAsync();
        LowStockCount = lowStock.Count;

        var expiring = await _inventoryService.GetExpiringBatchesAsync(60);
        ExpiringSoonCount = expiring.Count;

        TodaysRevenue = await _billingService.GetTodaysRevenueAsync();
        TodaysDiscounts = await _billingService.GetTodaysDiscountsAsync();

        var userId = _session.CurrentUser?.UserId ?? 0;
        if (userId > 0)
        {
            MyTodaysRevenue = await _billingService.GetTodaysRevenueByUserAsync(userId);
            MyTodaysBillCount = await _billingService.GetTodaysBillCountAsync(userId);
        }

        if (_session.IsAdmin)
        {
            var report = await _reportService.GenerateSummaryAsync(DateTime.Today, DateTime.Today);
            ReceptionistSummaries.Clear();
            if (report.RevenueByReceptionist is not null)
            {
                foreach (var r in report.RevenueByReceptionist)
                    ReceptionistSummaries.Add(r);
            }
        }
    }
}

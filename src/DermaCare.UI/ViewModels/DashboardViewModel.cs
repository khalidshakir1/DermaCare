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

    public DashboardViewModel(
        ICurrentSession session,
        IAppointmentRepository appointmentRepository,
        IAppointmentService appointmentService,
        IInventoryService inventoryService,
        IBillingService billingService,
        IReportService reportService,
        INavigationService navigationService)
    {
        _session = session;
        _appointmentRepository = appointmentRepository;
        _appointmentService = appointmentService;
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
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(true, DateTime.Today)));
        OpenInventoryCommand = new RelayCommand(() => _navigationService.NavigateTo<InventoryViewModel>());
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

    public ICommand RefreshCommand { get; }
    public ICommand OpenSaleSummaryCommand { get; }
    public ICommand OpenPosCommand { get; }
    public ICommand OpenBookTokenCommand { get; }
    public ICommand OpenInventoryCommand { get; }
    public ICommand CallTokenCommand { get; }
    public ICommand CompleteTokenCommand { get; }

    private async Task LoadSummaryAsync()
    {
        var today = await _appointmentRepository.GetByDateAsync(DateTime.Today);
        TodaysAppointmentCount = today.Count;
        PendingQueueCount = today.Count(a => a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.InProgress);

        TodaysQueue.Clear();
        foreach (var a in today.Where(a => a.Status != AppointmentStatus.Cancelled))
            TodaysQueue.Add(a);

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

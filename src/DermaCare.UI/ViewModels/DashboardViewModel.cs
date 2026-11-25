using DermaCare.Core.Interfaces;

namespace DermaCare.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ICurrentSession _session;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IBillingService _billingService;
    private int _todaysAppointmentCount;
    private int _lowStockCount;
    private int _expiringSoonCount;
    private decimal _todaysRevenue;

    public DashboardViewModel(
        ICurrentSession session,
        IAppointmentRepository appointmentRepository,
        IInventoryService inventoryService,
        IBillingService billingService)
    {
        _session = session;
        _appointmentRepository = appointmentRepository;
        _inventoryService = inventoryService;
        _billingService = billingService;
        _ = LoadSummaryAsync();
    }

    public string WelcomeTitle => _session.CurrentUser is null
        ? "Welcome to DermaCare Clinic"
        : $"Welcome, {_session.CurrentUser.FullName}";

    public string Subtitle => $"Signed in as {_session.RoleName}.";

    public int TodaysAppointmentCount
    {
        get => _todaysAppointmentCount;
        private set => SetProperty(ref _todaysAppointmentCount, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        private set => SetProperty(ref _lowStockCount, value);
    }

    public int ExpiringSoonCount
    {
        get => _expiringSoonCount;
        private set => SetProperty(ref _expiringSoonCount, value);
    }

    public decimal TodaysRevenue
    {
        get => _todaysRevenue;
        private set => SetProperty(ref _todaysRevenue, value);
    }

    private async Task LoadSummaryAsync()
    {
        var today = await _appointmentRepository.GetByDateAsync(DateTime.Today);
        TodaysAppointmentCount = today.Count;

        var lowStock = await _inventoryService.GetLowStockMedicinesAsync();
        LowStockCount = lowStock.Count;

        var expiring = await _inventoryService.GetExpiringBatchesAsync(60);
        ExpiringSoonCount = expiring.Count;

        TodaysRevenue = await _billingService.GetTodaysRevenueAsync();
    }
}

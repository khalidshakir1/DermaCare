using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Reports;
using DermaCare.UI.Commands;
using Microsoft.Win32;

namespace DermaCare.UI.ViewModels.Reports;

public class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly IBillingService _billingService;
    private readonly ExcelReportExporter _excelExporter;
    private readonly PdfReportExporter _pdfExporter;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private DateTime _fromDate = DateTime.Today.AddDays(-6);
    private DateTime _toDate = DateTime.Today;
    private string? _supplierFilter;
    private ClinicReportSummary? _summary;
    private string? _statusMessage;

    public ReportsViewModel(
        IReportService reportService,
        IBillingService billingService,
        ExcelReportExporter excelExporter,
        PdfReportExporter pdfExporter)
    {
        _reportService = reportService;
        _billingService = billingService;
        _excelExporter = excelExporter;
        _pdfExporter = pdfExporter;

        Appointments = new ObservableCollection<Appointment>();
        Invoices = new ObservableCollection<Invoice>();
        RevenueByMethod = new ObservableCollection<PaymentMethodSummary>();
        SupplierReports = new ObservableCollection<SupplierReportSummary>();
        SupplierBatchDetails = new ObservableCollection<SupplierBatchDetail>();

        DailyClosingCommand = new RelayCommand(async () =>
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await LoadReportAsync();
        });
        WeeklyReportCommand = new RelayCommand(async () =>
        {
            FromDate = DateTime.Today.AddDays(-6);
            ToDate = DateTime.Today;
            await LoadReportAsync();
        });
        MonthlyReportCommand = new RelayCommand(async () =>
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await LoadReportAsync();
        });

        GenerateCommand = new RelayCommand(async () => await LoadReportAsync());
        ExportExcelCommand = new RelayCommand(ExportExcel, () => _summary is not null);
        ExportPdfCommand = new RelayCommand(async () => await ExportPdfAsync(), () => _summary is not null);

        _ = LoadReportAsync();
    }

    public ObservableCollection<Appointment> Appointments { get; }
    public ObservableCollection<Invoice> Invoices { get; }
    public ObservableCollection<PaymentMethodSummary> RevenueByMethod { get; }
    public ObservableCollection<SupplierReportSummary> SupplierReports { get; }
    public ObservableCollection<SupplierBatchDetail> SupplierBatchDetails { get; }

    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
    public string? SupplierFilter { get => _supplierFilter; set => SetProperty(ref _supplierFilter, value); }
    public decimal TotalSupplierPurchaseCost => SupplierReports.Sum(s => s.PurchaseCost);
    public int TotalSupplierReturnedUnits => SupplierReports.Sum(s => s.UnitsReturned);

    public int TotalAppointments => _summary?.TotalAppointments ?? 0;
    public int CompletedAppointments => _summary?.CompletedAppointments ?? 0;
    public int NewPatients => _summary?.NewPatients ?? 0;
    public decimal TotalBilled => _summary?.TotalBilled ?? 0m;
    public decimal TotalCollected => _summary?.TotalCollected ?? 0m;

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand DailyClosingCommand { get; }
    public ICommand WeeklyReportCommand { get; }
    public ICommand MonthlyReportCommand { get; }
    public ICommand GenerateCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }

    private async Task LoadReportAsync()
    {
        if (!await _loadLock.WaitAsync(0)) return;
        try
        {
            _summary = await _reportService.GenerateSummaryAsync(FromDate, ToDate);

            Appointments.Clear();
            foreach (var a in _summary.Appointments) Appointments.Add(a);

            Invoices.Clear();
            foreach (var i in _summary.Invoices) Invoices.Add(i);

            RevenueByMethod.Clear();
            foreach (var m in _summary.RevenueByMethod) RevenueByMethod.Add(m);

            var supplierRows = await _reportService.GetSupplierReportAsync(FromDate, ToDate, SupplierFilter);
            SupplierReports.Clear();
            foreach (var s in supplierRows) SupplierReports.Add(s);

            var detailRows = await _reportService.GetSupplierBatchDetailsAsync(FromDate, ToDate, SupplierFilter);
            SupplierBatchDetails.Clear();
            foreach (var d in detailRows) SupplierBatchDetails.Add(d);

            OnPropertyChanged(nameof(TotalAppointments));
            OnPropertyChanged(nameof(CompletedAppointments));
            OnPropertyChanged(nameof(NewPatients));
            OnPropertyChanged(nameof(TotalBilled));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalSupplierPurchaseCost));
            OnPropertyChanged(nameof(TotalSupplierReturnedUnits));
            StatusMessage = $"Showing report for {FromDate:dd/MM/yyyy} – {ToDate:dd/MM/yyyy}.";
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void ExportExcel()
    {
        if (_summary is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export Clinic Report to Excel",
            Filter = "Excel Workbook|*.xlsx",
            FileName = $"DermaCare_Report_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;
        _excelExporter.ExportSummary(_summary, dialog.FileName);
        StatusMessage = $"Report exported to {dialog.FileName}";
    }

    private async Task ExportPdfAsync()
    {
        if (_summary is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export Clinic Report PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"DermaCare_Report_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog() != true) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        _pdfExporter.ExportSummaryReport(_summary, FromDate, ToDate, clinic, dialog.FileName);
        StatusMessage = $"PDF report exported to {dialog.FileName}";
    }
}

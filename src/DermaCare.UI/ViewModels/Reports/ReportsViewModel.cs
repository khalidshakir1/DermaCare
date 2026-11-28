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
    private string? _companyFilter;
    private bool _groupByVendor;
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
        CompanyPurchases = new ObservableCollection<CompanyPurchaseSummary>();
        BrandVendorDetails = new ObservableCollection<BrandVendorPurchaseDetail>();

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
    public ObservableCollection<CompanyPurchaseSummary> CompanyPurchases { get; }
    public ObservableCollection<BrandVendorPurchaseDetail> BrandVendorDetails { get; }

    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
    public string? CompanyFilter { get => _companyFilter; set => SetProperty(ref _companyFilter, value); }
    public bool GroupByVendor
    {
        get => _groupByVendor;
        set
        {
            if (SetProperty(ref _groupByVendor, value))
            {
                OnPropertyChanged(nameof(GroupByBrand));
                _ = LoadReportAsync();
            }
        }
    }
    public bool GroupByBrand
    {
        get => !_groupByVendor;
        set
        {
            if (value) GroupByVendor = false;
        }
    }
    public decimal TotalCompanyPurchaseCost => CompanyPurchases.Sum(c => c.TotalPurchaseCost);

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

            var companyRows = await _reportService.GetCompanyPurchasesAsync(FromDate, ToDate, CompanyFilter, GroupByVendor);
            CompanyPurchases.Clear();
            foreach (var c in companyRows) CompanyPurchases.Add(c);

            var detailRows = await _reportService.GetBrandVendorPurchaseDetailsAsync(FromDate, ToDate, CompanyFilter);
            BrandVendorDetails.Clear();
            foreach (var d in detailRows) BrandVendorDetails.Add(d);

            OnPropertyChanged(nameof(TotalAppointments));
            OnPropertyChanged(nameof(CompletedAppointments));
            OnPropertyChanged(nameof(NewPatients));
            OnPropertyChanged(nameof(TotalBilled));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalCompanyPurchaseCost));
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

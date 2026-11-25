using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Reports;
using DermaCare.UI.Commands;
using Microsoft.Win32;

namespace DermaCare.UI.ViewModels.Reports;

public class SaleSummaryViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly IBillingService _billingService;
    private readonly PdfReportExporter _pdfExporter;
    private readonly ExcelReportExporter _excelExporter;

    private DateTime _fromDate = DateTime.Today;
    private DateTime _toDate = DateTime.Today;
    private ClinicReportSummary? _summary;
    private string? _statusMessage;

    public SaleSummaryViewModel(
        IReportService reportService,
        IBillingService billingService,
        PdfReportExporter pdfExporter,
        ExcelReportExporter excelExporter)
    {
        _reportService = reportService;
        _billingService = billingService;
        _pdfExporter = pdfExporter;
        _excelExporter = excelExporter;

        RevenueByMethod = new ObservableCollection<PaymentMethodSummary>();
        RevenueByReceptionist = new ObservableCollection<ReceptionistRevenueSummary>();
        Invoices = new ObservableCollection<Invoice>();

        TodayCommand = new RelayCommand(async () =>
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await LoadSummaryAsync();
        });
        YesterdayCommand = new RelayCommand(async () =>
        {
            var y = DateTime.Today.AddDays(-1);
            FromDate = y;
            ToDate = y;
            await LoadSummaryAsync();
        });
        ThisWeekCommand = new RelayCommand(async () =>
        {
            FromDate = DateTime.Today.AddDays(-6);
            ToDate = DateTime.Today;
            await LoadSummaryAsync();
        });
        ThisMonthCommand = new RelayCommand(async () =>
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await LoadSummaryAsync();
        });
        GenerateCommand = new RelayCommand(async () => await LoadSummaryAsync());
        ExportPdfCommand = new RelayCommand(async () => await ExportPdfAsync(), () => _summary is not null);
        ExportExcelCommand = new RelayCommand(ExportExcel, () => _summary is not null);

        _ = LoadSummaryAsync();
    }

    public ObservableCollection<PaymentMethodSummary> RevenueByMethod { get; }
    public ObservableCollection<ReceptionistRevenueSummary> RevenueByReceptionist { get; }
    public ObservableCollection<Invoice> Invoices { get; }

    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

    public decimal TotalBilled => _summary?.TotalBilled ?? 0m;
    public decimal TotalCollected => _summary?.TotalCollected ?? 0m;
    public decimal TotalDiscount => _summary?.TotalDiscount ?? 0m;
    public int TotalBills => _summary?.Invoices.Count ?? 0;
    public int TotalAppointments => _summary?.TotalAppointments ?? 0;
    public int CompletedAppointments => _summary?.CompletedAppointments ?? 0;

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand TodayCommand { get; }
    public ICommand YesterdayCommand { get; }
    public ICommand ThisWeekCommand { get; }
    public ICommand ThisMonthCommand { get; }
    public ICommand GenerateCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportExcelCommand { get; }

    private async Task LoadSummaryAsync()
    {
        _summary = await _reportService.GenerateSummaryAsync(FromDate, ToDate);

        RevenueByMethod.Clear();
        foreach (var m in _summary.RevenueByMethod)
            RevenueByMethod.Add(m);

        RevenueByReceptionist.Clear();
        if (_summary.RevenueByReceptionist is not null)
        {
            foreach (var r in _summary.RevenueByReceptionist)
                RevenueByReceptionist.Add(r);
        }

        Invoices.Clear();
        foreach (var inv in _summary.Invoices)
            Invoices.Add(inv);

        OnPropertyChanged(nameof(TotalBilled));
        OnPropertyChanged(nameof(TotalCollected));
        OnPropertyChanged(nameof(TotalDiscount));
        OnPropertyChanged(nameof(TotalBills));
        OnPropertyChanged(nameof(TotalAppointments));
        OnPropertyChanged(nameof(CompletedAppointments));

        StatusMessage = $"Sale summary for {FromDate:dd/MM/yyyy} – {ToDate:dd/MM/yyyy}";
    }

    private async Task ExportPdfAsync()
    {
        if (_summary is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export Sale Summary PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"DermaCare_SaleSummary_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog() != true) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        _pdfExporter.ExportSummaryReport(_summary, FromDate, ToDate, clinic, dialog.FileName);
        StatusMessage = $"PDF exported to {dialog.FileName}";
    }

    private void ExportExcel()
    {
        if (_summary is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export Sale Summary Excel",
            Filter = "Excel Workbook|*.xlsx",
            FileName = $"DermaCare_SaleSummary_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        _excelExporter.ExportSummary(_summary, dialog.FileName);
        StatusMessage = $"Excel exported to {dialog.FileName}";
    }
}

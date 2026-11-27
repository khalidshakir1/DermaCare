using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Reports;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using Microsoft.Win32;

namespace DermaCare.UI.ViewModels.Billing;

public class InvoiceViewModel : BaseViewModel
{
    private readonly IBillingService _billingService;
    private readonly IInventoryService _inventoryService;
    private readonly PdfReportExporter _pdfExporter;

    private List<Invoice> _allRecentInvoices = new();
    private string? _customerName;
    private string? _billSearch;
    private ServiceItem? _selectedService;
    private Medicine? _selectedMedicine;
    private int _serviceQuantity = 1;
    private int _medicineQuantity = 1;
    private decimal _consultationFee = 2500m;
    private string _customDescription = string.Empty;
    private decimal _customPrice;
    private int _customQuantity = 1;
    private string? _notes;
    private decimal _discountAmount;
    private decimal _discountPercent;
    private string? _discountReason;
    private decimal _initialPaymentAmount;
    private PaymentMethod _initialPaymentMethod = PaymentMethod.Cash;
    private decimal _paymentAmount;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private Invoice? _selectedInvoice;
    private string? _statusMessage;
    private int? _preselectedPatientId;

    public InvoiceViewModel(
        IBillingService billingService,
        IInventoryService inventoryService,
        PdfReportExporter pdfExporter)
    {
        _billingService = billingService;
        _inventoryService = inventoryService;
        _pdfExporter = pdfExporter;

        Services = new ObservableCollection<ServiceItem>();
        Medicines = new ObservableCollection<Medicine>();
        LineItems = new ObservableCollection<InvoiceItem>();
        Invoices = new ObservableCollection<Invoice>();
        PaymentMethods = new[]
        {
            PaymentMethod.Cash,
            PaymentMethod.Card,
            PaymentMethod.EasyPaisa,
            PaymentMethod.JazzCash,
            PaymentMethod.BankTransfer
        };

        AddConsultationCommand = new RelayCommand(AddConsultation);
        AddServiceCommand = new RelayCommand(AddService);
        AddMedicineCommand = new RelayCommand(AddMedicine);
        AddCustomItemCommand = new RelayCommand(AddCustomItem);
        RemoveLineItemCommand = new RelayCommand(RemoveLineItem);
        ApplyFullDiscountCommand = new RelayCommand(() =>
        {
            DiscountAmount = 0m;
            DiscountPercent = 100m;
            if (string.IsNullOrWhiteSpace(DiscountReason))
                DiscountReason = "Full Discount (Dr. Reference)";
        });
        ApplyPercentPresetCommand = new RelayCommand(p =>
        {
            if (decimal.TryParse(p?.ToString(), out var pct))
            {
                DiscountAmount = 0m;
                DiscountPercent = pct;
                if (string.IsNullOrWhiteSpace(DiscountReason))
                    DiscountReason = $"{pct:N0}% Discount (Dr. Reference)";
            }
        });
        FillInitialFullCommand = new RelayCommand(() => InitialPaymentAmount = DraftTotal);
        CreateInvoiceCommand = new RelayCommand(async () => await CreateInvoiceAsync());
        FillBalanceCommand = new RelayCommand(() =>
        {
            if (SelectedInvoice is not null)
                PaymentAmount = SelectedInvoice.BalanceDue;
        });
        RecordPaymentCommand = new RelayCommand(async () => await RecordPaymentAsync(), () => SelectedInvoice is not null);
        PrintBillCommand = new RelayCommand(async () => await PrintBillAsync(), () => SelectedInvoice is not null);
        ExportPdfCommand = new RelayCommand(async () => await ExportInvoicePdfAsync(), () => SelectedInvoice is not null);
        RefreshCommand = new RelayCommand(async () => await InitializeAsync());

        _ = InitializeAsync();
    }

    public ObservableCollection<ServiceItem> Services { get; }
    public ObservableCollection<Medicine> Medicines { get; }
    public ObservableCollection<InvoiceItem> LineItems { get; }
    public ObservableCollection<Invoice> Invoices { get; }
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; }

    public string? CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string? BillSearch
    {
        get => _billSearch;
        set
        {
            if (SetProperty(ref _billSearch, value))
                FilterBills();
        }
    }

    public ServiceItem? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }
    public Medicine? SelectedMedicine { get => _selectedMedicine; set => SetProperty(ref _selectedMedicine, value); }
    public int ServiceQuantity { get => _serviceQuantity; set => SetProperty(ref _serviceQuantity, value); }
    public int MedicineQuantity { get => _medicineQuantity; set => SetProperty(ref _medicineQuantity, value); }
    public decimal ConsultationFee { get => _consultationFee; set => SetProperty(ref _consultationFee, value); }
    public string CustomDescription { get => _customDescription; set => SetProperty(ref _customDescription, value); }
    public decimal CustomPrice { get => _customPrice; set => SetProperty(ref _customPrice, value); }
    public int CustomQuantity { get => _customQuantity; set => SetProperty(ref _customQuantity, value); }
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (SetProperty(ref _discountAmount, value))
                OnPropertyChanged(nameof(DraftTotal));
        }
    }
    public decimal DiscountPercent
    {
        get => _discountPercent;
        set
        {
            if (SetProperty(ref _discountPercent, value))
                OnPropertyChanged(nameof(DraftTotal));
        }
    }
    public string? DiscountReason { get => _discountReason; set => SetProperty(ref _discountReason, value); }
    public decimal InitialPaymentAmount { get => _initialPaymentAmount; set => SetProperty(ref _initialPaymentAmount, value); }
    public PaymentMethod InitialPaymentMethod { get => _initialPaymentMethod; set => SetProperty(ref _initialPaymentMethod, value); }
    public decimal PaymentAmount { get => _paymentAmount; set => SetProperty(ref _paymentAmount, value); }
    public PaymentMethod PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }

    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value) && value is not null)
                PaymentAmount = value.BalanceDue;
        }
    }

    public decimal DraftSubtotal => LineItems.Sum(i => i.Quantity * i.UnitPrice);
    public decimal DraftTotal
    {
        get
        {
            var sub = DraftSubtotal;
            var disc = Math.Max(0m, DiscountAmount) + (sub * Math.Clamp(DiscountPercent, 0m, 100m) / 100m);
            return Math.Max(0m, sub - Math.Min(sub, disc));
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand AddConsultationCommand { get; }
    public ICommand AddServiceCommand { get; }
    public ICommand AddMedicineCommand { get; }
    public ICommand AddCustomItemCommand { get; }
    public ICommand RemoveLineItemCommand { get; }
    public ICommand ApplyFullDiscountCommand { get; }
    public ICommand ApplyPercentPresetCommand { get; }
    public ICommand FillInitialFullCommand { get; }
    public ICommand CreateInvoiceCommand { get; }
    public ICommand FillBalanceCommand { get; }
    public ICommand RecordPaymentCommand { get; }
    public ICommand PrintBillCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand RefreshCommand { get; }

    public void PreselectCustomer(string? customerName, int? patientId = null)
    {
        CustomerName = customerName;
        _preselectedPatientId = patientId;
    }

    private async Task InitializeAsync()
    {
        var services = await _billingService.GetActiveServicesAsync();
        Services.Clear();
        foreach (var s in services) Services.Add(s);
        SelectedService = Services.FirstOrDefault();

        var medicines = await _inventoryService.GetMedicinesAsync();
        Medicines.Clear();
        foreach (var m in medicines) Medicines.Add(m);
        SelectedMedicine = Medicines.FirstOrDefault();

        await LoadRecentInvoicesAsync();
    }

    private async Task LoadRecentInvoicesAsync()
    {
        _allRecentInvoices = (await _billingService.GetRecentInvoicesAsync(100)).ToList();
        FilterBills();
    }

    private void FilterBills()
    {
        var prevId = SelectedInvoice?.InvoiceId;
        Invoices.Clear();
        var q = BillSearch?.Trim();
        var matches = string.IsNullOrWhiteSpace(q)
            ? _allRecentInvoices
            : _allRecentInvoices.Where(i =>
                i.BillNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.DisplayCustomerName.Contains(q, StringComparison.OrdinalIgnoreCase));

        foreach (var inv in matches)
            Invoices.Add(inv);

        SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == prevId) ?? Invoices.FirstOrDefault();
    }

    private void AddConsultation()
    {
        if (ConsultationFee < 0) return;
        LineItems.Add(new InvoiceItem
        {
            ItemType = BillItemType.Consultation,
            Description = "Doctor Consultation Fee",
            Quantity = 1,
            UnitPrice = ConsultationFee
        });
        OnPropertyChanged(nameof(DraftSubtotal));
        OnPropertyChanged(nameof(DraftTotal));
    }

    private void AddService()
    {
        if (SelectedService is null || ServiceQuantity <= 0) return;
        LineItems.Add(new InvoiceItem
        {
            ItemType = BillItemType.Service,
            ServiceId = SelectedService.ServiceId,
            Description = SelectedService.ServiceName,
            Quantity = ServiceQuantity,
            UnitPrice = SelectedService.DefaultPrice
        });
        OnPropertyChanged(nameof(DraftSubtotal));
        OnPropertyChanged(nameof(DraftTotal));
    }

    private void AddMedicine()
    {
        if (SelectedMedicine is null || MedicineQuantity <= 0) return;
        if (SelectedMedicine.CurrentStock < MedicineQuantity)
        {
            StatusMessage = $"Insufficient stock for {SelectedMedicine.Name} (Available: {SelectedMedicine.CurrentStock}).";
            return;
        }

        LineItems.Add(new InvoiceItem
        {
            ItemType = BillItemType.Medicine,
            MedicineId = SelectedMedicine.MedicineId,
            Description = SelectedMedicine.Name,
            Quantity = MedicineQuantity,
            UnitPrice = SelectedMedicine.SellingPrice
        });
        StatusMessage = null;
        OnPropertyChanged(nameof(DraftSubtotal));
        OnPropertyChanged(nameof(DraftTotal));
    }

    private void AddCustomItem()
    {
        if (string.IsNullOrWhiteSpace(CustomDescription) || CustomQuantity <= 0 || CustomPrice < 0)
        {
            StatusMessage = "Enter a valid custom item description, quantity, and price.";
            return;
        }

        LineItems.Add(new InvoiceItem
        {
            ItemType = BillItemType.Custom,
            Description = CustomDescription.Trim(),
            Quantity = CustomQuantity,
            UnitPrice = CustomPrice
        });
        CustomDescription = string.Empty;
        CustomPrice = 0m;
        CustomQuantity = 1;
        StatusMessage = null;
        OnPropertyChanged(nameof(DraftSubtotal));
        OnPropertyChanged(nameof(DraftTotal));
    }

    private void RemoveLineItem(object? parameter)
    {
        if (parameter is InvoiceItem item && LineItems.Remove(item))
        {
            OnPropertyChanged(nameof(DraftSubtotal));
            OnPropertyChanged(nameof(DraftTotal));
        }
    }

    private async Task CreateInvoiceAsync()
    {
        var result = await _billingService.CreateInvoiceAsync(
            _preselectedPatientId,
            null,
            LineItems.ToList(),
            Notes,
            InitialPaymentAmount,
            InitialPaymentMethod,
            DiscountAmount,
            DiscountPercent,
            DiscountReason,
            CustomerName);

        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        LineItems.Clear();
        CustomerName = string.Empty;
        _preselectedPatientId = null;
        Notes = string.Empty;
        DiscountAmount = 0m;
        DiscountPercent = 0m;
        DiscountReason = string.Empty;
        InitialPaymentAmount = 0m;
        OnPropertyChanged(nameof(DraftSubtotal));
        OnPropertyChanged(nameof(DraftTotal));
        StatusMessage = $"Bill {result.Data?.BillNumber} created ({result.Data?.Status}).";

        // Refresh medicines (stock deducted) and bills list
        var medicines = await _inventoryService.GetMedicinesAsync();
        Medicines.Clear();
        foreach (var m in medicines) Medicines.Add(m);
        SelectedMedicine = Medicines.FirstOrDefault();

        await LoadRecentInvoicesAsync();
        SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == result.Data?.InvoiceId);
    }

    private async Task RecordPaymentAsync()
    {
        if (SelectedInvoice is null) return;
        var targetId = SelectedInvoice.InvoiceId;
        var result = await _billingService.RecordPaymentAsync(targetId, PaymentAmount, PaymentMethod);
        StatusMessage = result.Success ? $"Payment recorded on {SelectedInvoice.BillNumber}." : result.ErrorMessage;
        await LoadRecentInvoicesAsync();
        SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == targetId);
    }

    private async Task PrintBillAsync()
    {
        if (SelectedInvoice is null) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        if (ReceiptPrintHelper.PrintThermalBill(SelectedInvoice, clinic))
            StatusMessage = $"Sent {SelectedInvoice.BillNumber} to printer.";
    }

    private async Task ExportInvoicePdfAsync()
    {
        if (SelectedInvoice is null) return;
        var billLabel = string.IsNullOrWhiteSpace(SelectedInvoice.BillNumber)
            ? $"Invoice_{SelectedInvoice.InvoiceId}"
            : SelectedInvoice.BillNumber;

        var dialog = new SaveFileDialog
        {
            Title = "Save Bill Receipt PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"{billLabel}.pdf"
        };

        if (dialog.ShowDialog() != true) return;

        var clinic = await _billingService.GetClinicProfileAsync();
        _pdfExporter.ExportInvoice(SelectedInvoice, clinic, dialog.FileName);
        StatusMessage = $"Receipt exported to {dialog.FileName}";
    }
}

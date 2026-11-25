using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;

namespace DermaCare.UI.ViewModels.Inventory;

public class InventoryViewModel : BaseViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentSession _session;

    private string _searchQuery = string.Empty;
    private Medicine? _selectedMedicine;

    // Add / Edit Medicine fields
    private string _medName = string.Empty;
    private string _medGeneric = string.Empty;
    private string _medCategory = "Cream";
    private string _medUnit = "Tube";
    private int _medMinStock = 10;
    private decimal _medPurchasePrice;
    private decimal _medSellingPrice;

    // Stock In (Purchase) fields
    private string _batchNumber = string.Empty;
    private DateTime _expiryDate = DateTime.Today.AddMonths(12);
    private int _purchaseQuantity = 10;
    private decimal _batchPurchasePrice;
    private decimal _batchSellingPrice;
    private string _supplier = string.Empty;

    // Dispense / Adjust fields
    private int _adjustQuantity = 1;
    private StockTransactionType _adjustType = StockTransactionType.Dispensed;
    private string _adjustNotes = string.Empty;

    private int _lowStockCount;
    private int _expiringCount;
    private decimal _totalStockValue;
    private string? _statusMessage;

    public InventoryViewModel(IInventoryService inventoryService, ICurrentSession session)
    {
        _inventoryService = inventoryService;
        _session = session;

        Medicines = new ObservableCollection<Medicine>();
        ExpiringBatches = new ObservableCollection<MedicineBatch>();
        Transactions = new ObservableCollection<StockTransaction>();

        Categories = new[] { "Cream", "Ointment", "Gel", "Lotion", "Tablet", "Capsule", "Syrup", "Shampoo", "Injection", "Soap/Wash" };
        Units = new[] { "Tube", "Bottle", "Pack", "Strip", "Vial", "Pcs" };
        AdjustTypes = new[] { StockTransactionType.Dispensed, StockTransactionType.Adjusted, StockTransactionType.Expired };

        SearchCommand = new RelayCommand(async () => await LoadMedicinesAsync());
        AddMedicineCommand = new RelayCommand(async () => await AddMedicineAsync());
        AddStockCommand = new RelayCommand(async () => await AddStockAsync(), () => SelectedMedicine is not null);
        AdjustStockCommand = new RelayCommand(async () => await AdjustStockAsync(), () => SelectedMedicine is not null);
        RefreshCommand = new RelayCommand(async () => await LoadAllAsync());

        _ = LoadAllAsync();
    }

    public ObservableCollection<Medicine> Medicines { get; }
    public ObservableCollection<MedicineBatch> ExpiringBatches { get; }
    public ObservableCollection<StockTransaction> Transactions { get; }

    public IReadOnlyList<string> Categories { get; }
    public IReadOnlyList<string> Units { get; }
    public IReadOnlyList<StockTransactionType> AdjustTypes { get; }

    public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value); }

    public Medicine? SelectedMedicine
    {
        get => _selectedMedicine;
        set
        {
            if (SetProperty(ref _selectedMedicine, value) && value is not null)
            {
                BatchPurchasePrice = value.PurchasePrice;
                BatchSellingPrice = value.SellingPrice;
            }
        }
    }

    public string MedName { get => _medName; set => SetProperty(ref _medName, value); }
    public string MedGeneric { get => _medGeneric; set => SetProperty(ref _medGeneric, value); }
    public string MedCategory { get => _medCategory; set => SetProperty(ref _medCategory, value); }
    public string MedUnit { get => _medUnit; set => SetProperty(ref _medUnit, value); }
    public int MedMinStock { get => _medMinStock; set => SetProperty(ref _medMinStock, value); }
    public decimal MedPurchasePrice { get => _medPurchasePrice; set => SetProperty(ref _medPurchasePrice, value); }
    public decimal MedSellingPrice { get => _medSellingPrice; set => SetProperty(ref _medSellingPrice, value); }

    public string BatchNumber { get => _batchNumber; set => SetProperty(ref _batchNumber, value); }
    public DateTime ExpiryDate { get => _expiryDate; set => SetProperty(ref _expiryDate, value); }
    public int PurchaseQuantity { get => _purchaseQuantity; set => SetProperty(ref _purchaseQuantity, value); }
    public decimal BatchPurchasePrice { get => _batchPurchasePrice; set => SetProperty(ref _batchPurchasePrice, value); }
    public decimal BatchSellingPrice { get => _batchSellingPrice; set => SetProperty(ref _batchSellingPrice, value); }
    public string Supplier { get => _supplier; set => SetProperty(ref _supplier, value); }

    public int AdjustQuantity { get => _adjustQuantity; set => SetProperty(ref _adjustQuantity, value); }
    public StockTransactionType AdjustType { get => _adjustType; set => SetProperty(ref _adjustType, value); }
    public string AdjustNotes { get => _adjustNotes; set => SetProperty(ref _adjustNotes, value); }

    public int LowStockCount { get => _lowStockCount; private set => SetProperty(ref _lowStockCount, value); }
    public int ExpiringCount { get => _expiringCount; private set => SetProperty(ref _expiringCount, value); }
    public decimal TotalStockValue { get => _totalStockValue; private set => SetProperty(ref _totalStockValue, value); }
    public string? StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand SearchCommand { get; }
    public ICommand AddMedicineCommand { get; }
    public ICommand AddStockCommand { get; }
    public ICommand AdjustStockCommand { get; }
    public ICommand RefreshCommand { get; }

    private async Task LoadAllAsync()
    {
        await LoadMedicinesAsync();

        ExpiringBatches.Clear();
        foreach (var b in await _inventoryService.GetExpiringBatchesAsync(60))
            ExpiringBatches.Add(b);
        ExpiringCount = ExpiringBatches.Count;

        Transactions.Clear();
        foreach (var t in await _inventoryService.GetTransactionsAsync())
            Transactions.Add(t);
    }

    private async Task LoadMedicinesAsync()
    {
        var list = await _inventoryService.GetMedicinesAsync(SearchQuery);
        Medicines.Clear();
        foreach (var m in list)
            Medicines.Add(m);

        LowStockCount = Medicines.Count(m => m.CurrentStock <= m.MinStockLevel);
        TotalStockValue = Medicines.Sum(m => m.CurrentStock * m.PurchasePrice);
    }

    private async Task AddMedicineAsync()
    {
        var med = new Medicine
        {
            Name = MedName,
            GenericName = MedGeneric,
            Category = MedCategory,
            Unit = MedUnit,
            MinStockLevel = MedMinStock,
            PurchasePrice = MedPurchasePrice,
            SellingPrice = MedSellingPrice
        };

        var result = await _inventoryService.SaveMedicineAsync(med);
        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        MedName = string.Empty;
        MedGeneric = string.Empty;
        MedPurchasePrice = 0;
        MedSellingPrice = 0;
        StatusMessage = $"Added medicine '{result.Data!.Name}'.";
        await LoadAllAsync();
    }

    private async Task AddStockAsync()
    {
        if (SelectedMedicine is null) return;

        var userId = _session.CurrentUser?.UserId ?? 0;
        var result = await _inventoryService.AddStockPurchaseAsync(
            SelectedMedicine.MedicineId,
            BatchNumber,
            ExpiryDate,
            PurchaseQuantity,
            BatchPurchasePrice,
            BatchSellingPrice,
            Supplier,
            null,
            userId);

        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        BatchNumber = string.Empty;
        Supplier = string.Empty;
        StatusMessage = $"Added {PurchaseQuantity} {SelectedMedicine.Unit} to {SelectedMedicine.Name}.";
        await LoadAllAsync();
    }

    private async Task AdjustStockAsync()
    {
        if (SelectedMedicine is null) return;

        var userId = _session.CurrentUser?.UserId ?? 0;
        var result = await _inventoryService.AdjustOrDispenseAsync(
            SelectedMedicine.MedicineId,
            AdjustType,
            AdjustQuantity,
            AdjustNotes,
            userId);

        if (!result.Success)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        AdjustNotes = string.Empty;
        StatusMessage = $"Recorded {AdjustType} ({AdjustQuantity}) for {SelectedMedicine.Name}.";
        await LoadAllAsync();
    }
}
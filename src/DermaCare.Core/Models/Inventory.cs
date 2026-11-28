namespace DermaCare.Core.Models;

public enum StockTransactionType
{
    Purchase = 1,
    Dispensed = 2,
    Adjusted = 3,
    Expired = 4
}

public class Medicine
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Company { get; set; }
    public string Category { get; set; } = "Cream";
    public string Unit { get; set; } = "Tube";
    public int MinStockLevel { get; set; } = 10;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int CurrentStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}

public class MedicineBatch
{
    public int BatchId { get; set; }
    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public int QuantityRemaining { get; set; }
    public decimal PurchasePrice { get; set; }
    public string? Supplier { get; set; }

    public Medicine? Medicine { get; set; }
}

public class StockTransaction
{
    public int TransactionId { get; set; }
    public int MedicineId { get; set; }
    public int? BatchId { get; set; }
    public StockTransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int UserId { get; set; }

    public Medicine? Medicine { get; set; }
}

using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<Medicine>> GetMedicinesAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default);
    Task<ServiceResult<Medicine>> SaveMedicineAsync(Medicine medicine, CancellationToken ct = default);
    Task<ServiceResult<MedicineBatch>> AddStockPurchaseAsync(
        int medicineId,
        string batchNumber,
        DateTime expiryDate,
        int quantity,
        decimal purchasePrice,
        decimal? sellingPrice,
        string? supplier,
        string? notes,
        int userId,
        CancellationToken ct = default);
    Task<ServiceResult<Medicine>> AdjustOrDispenseAsync(
        int medicineId,
        StockTransactionType type,
        int quantity,
        string? notes,
        int userId,
        CancellationToken ct = default);
    Task<IReadOnlyList<MedicineBatch>> GetExpiringBatchesAsync(int daysAhead = 60, CancellationToken ct = default);
    Task<IReadOnlyList<Medicine>> GetLowStockMedicinesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockTransaction>> GetTransactionsAsync(int? medicineId = null, int take = 100, CancellationToken ct = default);
}

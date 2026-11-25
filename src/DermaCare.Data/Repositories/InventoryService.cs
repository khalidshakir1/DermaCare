using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class InventoryService : IInventoryService
{
    private readonly DermaDbContext _context;

    public InventoryService(DermaDbContext context) => _context = context;

    public async Task<IReadOnlyList<Medicine>> GetMedicinesAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _context.Medicines.AsQueryable();
        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.GenericName != null && m.GenericName.ToLower().Contains(term)) ||
                m.Category.ToLower().Contains(term));
        }

        return await query.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<ServiceResult<Medicine>> SaveMedicineAsync(Medicine medicine, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(medicine.Name))
            return new ServiceResult<Medicine>(false, ErrorMessage: "Medicine name is required.");

        if (medicine.MinStockLevel < 0 || medicine.PurchasePrice < 0 || medicine.SellingPrice < 0)
            return new ServiceResult<Medicine>(false, ErrorMessage: "Stock level and prices cannot be negative.");

        medicine.Name = medicine.Name.Trim();
        medicine.GenericName = medicine.GenericName?.Trim();
        medicine.Category = string.IsNullOrWhiteSpace(medicine.Category) ? "Cream" : medicine.Category.Trim();
        medicine.Unit = string.IsNullOrWhiteSpace(medicine.Unit) ? "Tube" : medicine.Unit.Trim();

        if (medicine.MedicineId == 0)
        {
            medicine.CreatedAt = DateTime.UtcNow;
            _context.Medicines.Add(medicine);
        }
        else
        {
            _context.Medicines.Update(medicine);
        }

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<Medicine>(true, medicine);
    }

    public async Task<ServiceResult<MedicineBatch>> AddStockPurchaseAsync(
        int medicineId,
        string batchNumber,
        DateTime expiryDate,
        int quantity,
        decimal purchasePrice,
        decimal? sellingPrice,
        string? supplier,
        string? notes,
        int userId,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            return new ServiceResult<MedicineBatch>(false, ErrorMessage: "Quantity must be greater than zero.");

        if (string.IsNullOrWhiteSpace(batchNumber))
            return new ServiceResult<MedicineBatch>(false, ErrorMessage: "Batch number is required.");

        var medicine = await _context.Medicines.FindAsync(new object[] { medicineId }, ct);
        if (medicine is null)
            return new ServiceResult<MedicineBatch>(false, ErrorMessage: "Medicine not found.");

        var batch = new MedicineBatch
        {
            MedicineId = medicineId,
            BatchNumber = batchNumber.Trim(),
            ExpiryDate = expiryDate.Date,
            PurchaseDate = DateTime.UtcNow,
            QuantityRemaining = quantity,
            PurchasePrice = purchasePrice,
            Supplier = supplier?.Trim()
        };

        _context.MedicineBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        medicine.CurrentStock += quantity;
        if (purchasePrice > 0) medicine.PurchasePrice = purchasePrice;
        if (sellingPrice.HasValue && sellingPrice.Value > 0) medicine.SellingPrice = sellingPrice.Value;

        _context.StockTransactions.Add(new StockTransaction
        {
            MedicineId = medicineId,
            BatchId = batch.BatchId,
            TransactionType = StockTransactionType.Purchase,
            Quantity = quantity,
            TransactionDate = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(notes) ? $"Batch {batch.BatchNumber} ({batch.Supplier ?? "Supplier"})" : notes.Trim(),
            UserId = userId
        });

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<MedicineBatch>(true, batch);
    }

    public async Task<ServiceResult<Medicine>> AdjustOrDispenseAsync(
        int medicineId,
        StockTransactionType type,
        int quantity,
        string? notes,
        int userId,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            return new ServiceResult<Medicine>(false, ErrorMessage: "Quantity must be greater than zero.");

        var medicine = await _context.Medicines.FindAsync(new object[] { medicineId }, ct);
        if (medicine is null)
            return new ServiceResult<Medicine>(false, ErrorMessage: "Medicine not found.");

        if (type != StockTransactionType.Purchase && medicine.CurrentStock < quantity)
            return new ServiceResult<Medicine>(false, ErrorMessage: $"Insufficient stock. Available: {medicine.CurrentStock} {medicine.Unit}.");

        var signedQty = type == StockTransactionType.Purchase ? quantity : -quantity;
        medicine.CurrentStock += signedQty;

        // Deduct from earliest-expiring active batches (FEFO)
        if (signedQty < 0)
        {
            var remainingToDeduct = quantity;
            var batches = await _context.MedicineBatches
                .Where(b => b.MedicineId == medicineId && b.QuantityRemaining > 0)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync(ct);

            foreach (var batch in batches)
            {
                if (remainingToDeduct <= 0) break;
                var take = Math.Min(batch.QuantityRemaining, remainingToDeduct);
                batch.QuantityRemaining -= take;
                remainingToDeduct -= take;
            }
        }

        _context.StockTransactions.Add(new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = type,
            Quantity = signedQty,
            TransactionDate = DateTime.UtcNow,
            Notes = notes?.Trim(),
            UserId = userId
        });

        await _context.SaveChangesAsync(ct);
        return new ServiceResult<Medicine>(true, medicine);
    }

    public async Task<IReadOnlyList<MedicineBatch>> GetExpiringBatchesAsync(int daysAhead = 60, CancellationToken ct = default)
    {
        var cutoff = DateTime.Today.AddDays(daysAhead);
        return await _context.MedicineBatches
            .Include(b => b.Medicine)
            .Where(b => b.QuantityRemaining > 0 && b.ExpiryDate <= cutoff)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Medicine>> GetLowStockMedicinesAsync(CancellationToken ct = default) =>
        await _context.Medicines
            .Where(m => m.IsActive && m.CurrentStock <= m.MinStockLevel)
            .OrderBy(m => m.CurrentStock)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StockTransaction>> GetTransactionsAsync(int? medicineId = null, int take = 100, CancellationToken ct = default)
    {
        var query = _context.StockTransactions.Include(t => t.Medicine).AsQueryable();
        if (medicineId.HasValue)
            query = query.Where(t => t.MedicineId == medicineId.Value);

        return await query.OrderByDescending(t => t.TransactionDate).Take(take).ToListAsync(ct);
    }
}

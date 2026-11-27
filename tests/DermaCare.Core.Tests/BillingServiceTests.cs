using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class BillingServiceTests
{
    [Fact]
    public async Task CreateInvoiceAsync_CalculatesTotalGeneratesBillNumberAndSetsIssued()
    {
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.GenerateNextBillNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("BILL-0001");
        var session = new CurrentSession();
        var sut = new BillingService(repo.Object, session);

        var items = new[]
        {
            new InvoiceItem { ItemType = BillItemType.Consultation, Description = "Consultation", Quantity = 1, UnitPrice = 2500m },
            new InvoiceItem { ItemType = BillItemType.Service, Description = "Chemical Peel", Quantity = 2, UnitPrice = 1500m }
        };

        var result = await sut.CreateInvoiceAsync(1, null, items);

        Assert.True(result.Success);
        Assert.Equal("BILL-0001", result.Data?.BillNumber);
        Assert.Equal(5500m, result.Data?.TotalAmount);
        Assert.Equal(InvoiceStatus.Issued, result.Data?.Status);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithMedicine_DeductsInventoryStock()
    {
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.GenerateNextBillNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("BILL-0002");
        var inventory = new Mock<IInventoryService>();
        inventory.Setup(i => i.GetMedicinesAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Medicine { MedicineId = 5, Name = "Adapalene Gel", CurrentStock = 10 } });
        inventory.Setup(i => i.AdjustOrDispenseAsync(5, StockTransactionType.Dispensed, 2, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResult<Medicine>(true, new Medicine { MedicineId = 5, CurrentStock = 8 }));

        var sut = new BillingService(repo.Object, new CurrentSession(), inventory.Object);
        var items = new[]
        {
            new InvoiceItem { ItemType = BillItemType.Medicine, MedicineId = 5, Description = "Adapalene Gel", Quantity = 2, UnitPrice = 850m }
        };

        var result = await sut.CreateInvoiceAsync(1, null, items);

        Assert.True(result.Success);
        inventory.Verify(i => i.AdjustOrDispenseAsync(5, StockTransactionType.Dispensed, 2, "Billed on BILL-0002", 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithInsufficientMedicineStock_ReturnsFailure()
    {
        var repo = new Mock<IBillingRepository>();
        var inventory = new Mock<IInventoryService>();
        inventory.Setup(i => i.GetMedicinesAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Medicine { MedicineId = 5, Name = "Adapalene Gel", CurrentStock = 1 } });

        var sut = new BillingService(repo.Object, new CurrentSession(), inventory.Object);
        var items = new[]
        {
            new InvoiceItem { ItemType = BillItemType.Medicine, MedicineId = 5, Description = "Adapalene Gel", Quantity = 3, UnitPrice = 850m }
        };

        var result = await sut.CreateInvoiceAsync(1, null, items);

        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithFixedAndPercentDiscount_CalculatesNetTotal()
    {
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.GenerateNextBillNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("BILL-0003");
        var sut = new BillingService(repo.Object, new CurrentSession());

        var items = new[]
        {
            new InvoiceItem { ItemType = BillItemType.Consultation, Description = "Consultation", Quantity = 1, UnitPrice = 2000m }
        };

        // 10% of 2000 = 200, plus 300 fixed = 500 discount -> 1500 net
        var session = new CurrentSession();
        session.SetUser(new User { UserId = 7, Username = "recep1", Role = new Role { RoleName = "Receptionist" } });
        sut = new BillingService(repo.Object, session);
        var result = await sut.CreateInvoiceAsync(1, null, items, discountAmount: 300m, discountPercent: 10m, discountReason: "Dr. Reference");

        Assert.True(result.Success);
        Assert.Equal(2000m, result.Data?.Subtotal);
        Assert.Equal(500m, result.Data?.DiscountAmount);
        Assert.Equal(1500m, result.Data?.TotalAmount);
        Assert.Equal("Dr. Reference", result.Data?.DiscountReason);
        Assert.Equal(7, result.Data?.CollectedByUserId);
    }

    [Fact]
    public async Task RecordPaymentAsync_FullPayment_MarksInvoicePaid()
    {
        var invoice = new Invoice { InvoiceId = 10, BillNumber = "BILL-0010", TotalAmount = 200m, PaidAmount = 0m, Status = InvoiceStatus.Issued };
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.GetInvoiceByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        var sut = new BillingService(repo.Object, new CurrentSession());

        var result = await sut.RecordPaymentAsync(10, 200m, PaymentMethod.EasyPaisa);

        Assert.True(result.Success);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(200m, invoice.PaidAmount);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithoutPatientInfo_CreatesAnonymousWalkInBill()
    {
        var repo = new Mock<IBillingRepository>();
        repo.Setup(r => r.GenerateNextBillNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("BILL-0099");
        var sut = new BillingService(repo.Object, new CurrentSession());

        var items = new[]
        {
            new InvoiceItem { ItemType = BillItemType.Custom, Description = "Sunblock SPF 50+", Quantity = 1, UnitPrice = 1800m }
        };

        var result = await sut.CreateInvoiceAsync(null, null, items);

        Assert.True(result.Success);
        Assert.Null(result.Data?.PatientId);
        Assert.Equal("Walk-In Customer", result.Data?.DisplayCustomerName);
        Assert.Equal(1800m, result.Data?.TotalAmount);
    }
}

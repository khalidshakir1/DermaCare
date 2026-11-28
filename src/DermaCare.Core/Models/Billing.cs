namespace DermaCare.Core.Models;

public class ServiceItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public string Category { get; set; } = "General";
    public bool IsActive { get; set; } = true;
}

public class Invoice
{
    public int InvoiceId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public string? CustomerName { get; set; }
    public int? AppointmentId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? DiscountReason { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public int CollectedByUserId { get; set; }
    public string? Notes { get; set; }

    public decimal BalanceDue => Math.Max(0m, TotalAmount - PaidAmount);
    public string DisplayCustomerName => !string.IsNullOrWhiteSpace(CustomerName)
        ? CustomerName
        : (Patient?.FullName ?? "Walk-In Customer");

    public Patient? Patient { get; set; }
    public Appointment? Appointment { get; set; }
    public User? CollectedByUser { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class InvoiceItem
{
    public int ItemId { get; set; }
    public int InvoiceId { get; set; }
    public BillItemType ItemType { get; set; } = BillItemType.Service;
    public int? ServiceId { get; set; }
    public int? MedicineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }

    public decimal GrossTotal => Quantity * UnitPrice;
    public decimal EffectiveDiscount => Math.Min(GrossTotal, Math.Max(0m, DiscountAmount) + (GrossTotal * Math.Clamp(DiscountPercent, 0m, 100m) / 100m));
    public decimal LineTotal => Math.Max(0m, GrossTotal - EffectiveDiscount);

    public Invoice? Invoice { get; set; }
    public ServiceItem? Service { get; set; }
    public Medicine? Medicine { get; set; }
}

public class Payment
{
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public int ReceivedBy { get; set; }

    public Invoice? Invoice { get; set; }
}

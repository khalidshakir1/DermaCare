namespace DermaCare.Core.Models;

public enum AppointmentType
{
    Consultation = 1,
    FollowUp = 2,
    Procedure = 3,
    WalkIn = 4
}

public enum AppointmentStatus
{
    Scheduled = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    PartiallyPaid = 4,
    Void = 5
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    Insurance = 3,
    BankTransfer = 4,
    EasyPaisa = 5,
    JazzCash = 6
}

public enum BillItemType
{
    Consultation = 1,
    Service = 2,
    Medicine = 3,
    Custom = 4
}

public enum PhotoType
{
    Before = 1,
    After = 2,
    Diagnostic = 3
}

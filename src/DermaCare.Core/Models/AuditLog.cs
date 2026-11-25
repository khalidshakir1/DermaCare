namespace DermaCare.Core.Models;

public class AuditLog
{
    public int LogId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ClinicProfile
{
    public int ClinicProfileId { get; set; }
    public string ClinicName { get; set; } = "DermaCare Dermatology Clinic";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
}

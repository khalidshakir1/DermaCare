namespace DermaCare.Core.Models;

public class Patient
{
    public int PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CNIC { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? SkinType { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PatientPhoto> Photos { get; set; } = new List<PatientPhoto>();
}

public class PatientPhoto
{
    public int PhotoId { get; set; }
    public int PatientId { get; set; }
    public int? RecordId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public PhotoType PhotoType { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}


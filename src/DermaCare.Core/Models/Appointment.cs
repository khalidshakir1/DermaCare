namespace DermaCare.Core.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public AppointmentType Type { get; set; } = AppointmentType.Consultation;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string TokenNumber { get; set; } = string.Empty;
    public int TokenSequence { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayPatientName => !string.IsNullOrWhiteSpace(PatientName)
        ? PatientName
        : (Patient?.FullName ?? (Type == AppointmentType.WalkIn ? "Walk-In Patient" : "Guest"));

    public string DisplayPatientPhone => !string.IsNullOrWhiteSpace(PatientPhone)
        ? PatientPhone
        : (Patient?.Phone ?? string.Empty);

    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
    public Invoice? Invoice { get; set; }
}

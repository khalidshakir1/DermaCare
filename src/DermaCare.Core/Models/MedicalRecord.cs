namespace DermaCare.Core.Models;

public class MedicalRecord
{
    public int RecordId { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;
    public string? ICD10Code { get; set; }
    public string? ProcedureNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Appointment? Appointment { get; set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<PatientPhoto> Photos { get; set; } = new List<PatientPhoto>();
}

public class Prescription
{
    public int PrescriptionId { get; set; }
    public int RecordId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }
}

namespace DermaCare.Core.Models;

public class Doctor
{
    public int DoctorId { get; set; }
    public int UserId { get; set; }
    public string Specialization { get; set; } = "Dermatology";
    public string Phone { get; set; } = string.Empty;
    public string? PMDCNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}

public class DoctorSchedule
{
    public int ScheduleId { get; set; }
    public int DoctorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public Doctor? Doctor { get; set; }
}

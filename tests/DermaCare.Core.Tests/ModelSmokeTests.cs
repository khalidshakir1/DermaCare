using DermaCare.Core.Models;

namespace DermaCare.Core.Tests;

public class ModelSmokeTests
{
    [Fact]
    public void Patient_FullName_CombinesFirstAndLastName()
    {
        var patient = new Patient { FirstName = "Jane", LastName = "Doe" };
        Assert.Equal("Jane Doe", patient.FullName);
    }

    [Fact]
    public void Appointment_DefaultsToScheduledConsultation()
    {
        var appointment = new Appointment();
        Assert.Equal(AppointmentType.Consultation, appointment.Type);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }
}

using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class AppointmentServiceTests
{
    [Fact]
    public async Task BookAsync_ConflictingSlot_ReturnsError()
    {
        var apptRepo = new Mock<IAppointmentRepository>();
        var docRepo = new Mock<IDoctorRepository>();
        var docService = new Mock<IDoctorService>();
        var session = new CurrentSession();

        docRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor { DoctorId = 1 });
        apptRepo.Setup(r => r.HasConflictAsync(1, It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new AppointmentService(apptRepo.Object, docRepo.Object, docService.Object, session);
        var result = await sut.BookAsync(new Appointment
        {
            PatientId = 1,
            DoctorId = 1,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(10, 0, 0)
        });

        Assert.False(result.Success);
        Assert.Contains("already has an appointment", result.ErrorMessage);
    }

    [Fact]
    public async Task BookAsync_ValidSlot_AddsAppointmentAsScheduled()
    {
        var apptRepo = new Mock<IAppointmentRepository>();
        var docRepo = new Mock<IDoctorRepository>();
        var docService = new Mock<IDoctorService>();
        var session = new CurrentSession();

        docRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor { DoctorId = 1 });
        apptRepo.Setup(r => r.HasConflictAsync(1, It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        apptRepo.Setup(r => r.GetNextTokenSequenceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var sut = new AppointmentService(apptRepo.Object, docRepo.Object, docService.Object, session);
        var result = await sut.BookAsync(new Appointment
        {
            PatientId = 2,
            DoctorId = 1,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(11, 0, 0)
        });

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Scheduled, result.Data?.Status);
        Assert.Equal("T-004", result.Data?.TokenNumber);
        Assert.Equal(4, result.Data?.TokenSequence);
        apptRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

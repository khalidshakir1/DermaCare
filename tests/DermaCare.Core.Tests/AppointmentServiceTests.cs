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
        Assert.Contains("already exists", result.ErrorMessage);
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
        apptRepo.Setup(r => r.GetNextTokenSequenceAsync(It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var sut = new AppointmentService(apptRepo.Object, docRepo.Object, docService.Object, session);
        var result = await sut.BookAsync(new Appointment
        {
            PatientName = "Ayesha Khan",
            PatientPhone = "0300-1234567",
            DoctorId = 1,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(11, 0, 0)
        });

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Scheduled, result.Data?.Status);
        Assert.Equal(string.Empty, result.Data?.TokenNumber);
        Assert.Equal(0, result.Data?.TokenSequence);
        apptRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueAppointmentTokenAsync_BookedAppointment_AssignsPTokenAndConfirms()
    {
        var apptRepo = new Mock<IAppointmentRepository>();
        var docRepo = new Mock<IDoctorRepository>();
        var docService = new Mock<IDoctorService>();
        var session = new CurrentSession();

        var booked = new Appointment
        {
            AppointmentId = 10,
            PatientName = "Ayesha Khan",
            PatientPhone = "0300-1234567",
            DoctorId = 1,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(11, 0, 0),
            Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Scheduled,
            TokenNumber = string.Empty
        };

        apptRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(booked);
        apptRepo.Setup(r => r.GetNextTokenSequenceAsync(It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var sut = new AppointmentService(apptRepo.Object, docRepo.Object, docService.Object, session);
        var result = await sut.IssueAppointmentTokenAsync(10);

        Assert.True(result.Success);
        Assert.Equal("P-004", result.Data?.TokenNumber);
        Assert.Equal(4, result.Data?.TokenSequence);
        Assert.Equal(AppointmentStatus.Confirmed, result.Data?.Status);
        apptRepo.Verify(r => r.UpdateAsync(booked, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookAsync_WalkInZeroInfo_AutoAssignsDoctorAndWToken()
    {
        var apptRepo = new Mock<IAppointmentRepository>();
        var docRepo = new Mock<IDoctorRepository>();
        var docService = new Mock<IDoctorService>();
        var session = new CurrentSession();

        docRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Doctor { DoctorId = 1 } });
        apptRepo.Setup(r => r.GetNextTokenSequenceAsync(It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new AppointmentService(apptRepo.Object, docRepo.Object, docService.Object, session);
        var result = await sut.BookAsync(new Appointment
        {
            Type = AppointmentType.WalkIn,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(12, 15, 0)
        });

        Assert.True(result.Success);
        Assert.Equal("W-001", result.Data?.TokenNumber);
        Assert.Equal(1, result.Data?.DoctorId);
        Assert.Equal("Walk-In Patient", result.Data?.DisplayPatientName);
    }
}

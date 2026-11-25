using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class DoctorServiceTests
{
    [Fact]
    public async Task CreateAsync_InvalidScheduleRange_ReturnsError()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var userRepo = new Mock<IUserRepository>();
        var sut = new DoctorService(doctorRepo.Object, userRepo.Object);

        var doctor = new Doctor
        {
            UserId = 2,
            Specialization = "Cosmetic Dermatology",
            Schedules =
            {
                new DoctorSchedule
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(9, 0, 0)
                }
            }
        };

        var result = await sut.CreateAsync(doctor);

        Assert.False(result.Success);
        Assert.Contains("earlier than end time", result.ErrorMessage);
    }

    [Fact]
    public void IsWithinSchedule_MatchingDayAndTime_ReturnsTrue()
    {
        var doctor = new Doctor
        {
            Schedules =
            {
                new DoctorSchedule
                {
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0)
                }
            }
        };

        // 2026-11-25 is a Wednesday
        var date = new DateTime(2026, 11, 25);
        Assert.True(new DoctorService(Mock.Of<IDoctorRepository>(), Mock.Of<IUserRepository>())
            .IsWithinSchedule(doctor, date, new TimeSpan(10, 30, 0)));
    }
}

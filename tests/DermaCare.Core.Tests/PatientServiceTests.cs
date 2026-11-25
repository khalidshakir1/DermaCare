using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidPatient_GeneratesCodeAndSaves()
    {
        var repo = new Mock<IPatientRepository>();
        repo.Setup(r => r.GenerateNextPatientCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DERM-2026-00001");
        var sut = new PatientService(repo.Object);

        var patient = new Patient
        {
            FirstName = "Sarah",
            LastName = "Connor",
            Phone = "555-0199",
            Gender = "Female",
            DateOfBirth = new DateTime(1990, 5, 12)
        };

        var result = await sut.CreateAsync(patient);

        Assert.True(result.Success);
        Assert.Equal("DERM-2026-00001", result.Data?.PatientCode);
        Assert.True(result.Data?.IsActive);
        repo.Verify(r => r.AddAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MissingFirstName_ReturnsValidationError()
    {
        var repo = new Mock<IPatientRepository>();
        var sut = new PatientService(repo.Object);

        var patient = new Patient
        {
            FirstName = "",
            LastName = "Connor",
            Phone = "555-0199",
            Gender = "Female",
            DateOfBirth = new DateTime(1990, 5, 12)
        };

        var result = await sut.CreateAsync(patient);

        Assert.False(result.Success);
        Assert.Contains("First name", result.ErrorMessage);
        repo.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_ExistingPatient_SetsIsActiveFalse()
    {
        var existing = new Patient { PatientId = 7, FirstName = "John", LastName = "Doe", IsActive = true };
        var repo = new Mock<IPatientRepository>();
        repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var sut = new PatientService(repo.Object);

        var result = await sut.DeactivateAsync(7);

        Assert.True(result.Success);
        Assert.False(existing.IsActive);
        repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}

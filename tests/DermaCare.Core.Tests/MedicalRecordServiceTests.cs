using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Core.Services;
using Moq;

namespace DermaCare.Core.Tests;

public class MedicalRecordServiceTests
{
    [Fact]
    public async Task SaveConsultationAsync_ValidRecord_SavesAndReturnsSuccess()
    {
        var repo = new Mock<IMedicalRecordRepository>();
        var sut = new MedicalRecordService(repo.Object);

        var record = new MedicalRecord
        {
            AppointmentId = 1,
            PatientId = 2,
            DoctorId = 3,
            ChiefComplaint = "Facial acne flare-up",
            Diagnosis = "Acne vulgaris",
            ICD10Code = "L70.0"
        };

        var result = await sut.SaveConsultationAsync(record);

        Assert.True(result.Success);
        repo.Verify(r => r.AddAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveConsultationAsync_MissingDiagnosis_ReturnsError()
    {
        var repo = new Mock<IMedicalRecordRepository>();
        var sut = new MedicalRecordService(repo.Object);

        var result = await sut.SaveConsultationAsync(new MedicalRecord
        {
            AppointmentId = 1,
            PatientId = 2,
            ChiefComplaint = "Rash",
            Diagnosis = ""
        });

        Assert.False(result.Success);
        Assert.Contains("Diagnosis", result.ErrorMessage);
    }
}

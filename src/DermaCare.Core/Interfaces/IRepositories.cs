using DermaCare.Core.Models;

namespace DermaCare.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
    Task AddDoctorUserAsync(User user, Doctor doctor, CancellationToken ct = default);
}

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> SearchAsync(string? query, bool includeInactive = false, CancellationToken ct = default);
    Task<string> GenerateNextPatientCodeAsync(CancellationToken ct = default);
    Task AddAsync(Patient patient, CancellationToken ct = default);
    Task UpdateAsync(Patient patient, CancellationToken ct = default);
}

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken ct = default);
    Task<IReadOnlyList<Doctor>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(Doctor doctor, CancellationToken ct = default);
    Task UpdateAsync(Doctor doctor, CancellationToken ct = default);
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken ct = default);
    Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, int? doctorId = null, CancellationToken ct = default);
    Task<bool> HasConflictAsync(int doctorId, DateTime date, TimeSpan time, int? excludeAppointmentId = null, CancellationToken ct = default);
    Task AddAsync(Appointment appointment, CancellationToken ct = default);
    Task UpdateAsync(Appointment appointment, CancellationToken ct = default);
}

public interface IMedicalRecordRepository
{
    Task<MedicalRecord?> GetByIdAsync(int recordId, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId, CancellationToken ct = default);
    Task AddAsync(MedicalRecord record, CancellationToken ct = default);
    Task UpdateAsync(MedicalRecord record, CancellationToken ct = default);
}

public interface IBillingRepository
{
    Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetInvoicesByPatientAsync(int patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetRecentInvoicesAsync(int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceItem>> GetActiveServicesAsync(CancellationToken ct = default);
    Task<string> GenerateNextBillNumberAsync(CancellationToken ct = default);
    Task<decimal> GetTodaysRevenueAsync(CancellationToken ct = default);
    Task<ClinicProfile> GetClinicProfileAsync(CancellationToken ct = default);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task AddPaymentAsync(Payment payment, CancellationToken ct = default);
}


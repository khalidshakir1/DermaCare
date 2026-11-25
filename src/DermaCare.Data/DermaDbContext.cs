using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data;

public class DermaDbContext : DbContext
{
    public DermaDbContext(DbContextOptions<DermaDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientPhoto> PatientPhotos => Set<PatientPhoto>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ClinicProfile> ClinicProfiles => Set<ClinicProfile>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(60).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Patient>(e =>
        {
            e.HasKey(x => x.PatientId);
            e.Property(x => x.PatientCode).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.PatientCode).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            e.Property(x => x.Gender).HasMaxLength(20).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.SkinType).HasMaxLength(50);
            e.Property(x => x.CNIC).HasMaxLength(20);
        });

        modelBuilder.Entity<PatientPhoto>(e =>
        {
            e.HasKey(x => x.PhotoId);
            e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Patient).WithMany(p => p.Photos).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MedicalRecord).WithMany(m => m.Photos).HasForeignKey(x => x.RecordId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Doctor>(e =>
        {
            e.HasKey(x => x.DoctorId);
            e.Property(x => x.Specialization).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PMDCNumber).HasMaxLength(40);
            e.HasOne(x => x.User).WithOne(u => u.DoctorProfile).HasForeignKey<Doctor>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DoctorSchedule>(e =>
        {
            e.HasKey(x => x.ScheduleId);
            e.HasOne(x => x.Doctor).WithMany(d => d.Schedules).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasKey(x => x.AppointmentId);
            e.Property(x => x.TokenNumber).HasMaxLength(20);
            e.HasOne(x => x.Patient).WithMany(p => p.Appointments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor).WithMany(d => d.Appointments).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicalRecord>(e =>
        {
            e.HasKey(x => x.RecordId);
            e.Property(x => x.ICD10Code).HasMaxLength(20);
            e.HasOne(x => x.Appointment).WithOne(a => a.MedicalRecord).HasForeignKey<MedicalRecord>(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Patient).WithMany(p => p.MedicalRecords).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor).WithMany(d => d.MedicalRecords).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Prescription>(e =>
        {
            e.HasKey(x => x.PrescriptionId);
            e.Property(x => x.DrugName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Dose).HasMaxLength(80).IsRequired();
            e.Property(x => x.Frequency).HasMaxLength(80).IsRequired();
            e.Property(x => x.Duration).HasMaxLength(80).IsRequired();
            e.HasOne(x => x.MedicalRecord).WithMany(m => m.Prescriptions).HasForeignKey(x => x.RecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceItem>(e =>
        {
            e.HasKey(x => x.ServiceId);
            e.Property(x => x.ServiceName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.Property(x => x.DefaultPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.InvoiceId);
            e.Property(x => x.BillNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.DiscountReason).HasMaxLength(150);
            e.Property(x => x.Notes).HasMaxLength(300);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Ignore(x => x.BalanceDue);
            e.Ignore(x => x.CollectedByUser);
            e.HasOne(x => x.Patient).WithMany(p => p.Invoices).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Appointment).WithOne(a => a.Invoice).HasForeignKey<Invoice>(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(e =>
        {
            e.HasKey(x => x.ItemId);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Medicine).WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Invoice).WithMany(i => i.Payments).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.LogId);
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.TableName).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<ClinicProfile>(e =>
        {
            e.HasKey(x => x.ClinicProfileId);
            e.Property(x => x.ClinicName).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Medicine>(e =>
        {
            e.HasKey(x => x.MedicineId);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.GenericName).HasMaxLength(150);
            e.Property(x => x.Category).HasMaxLength(60).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(30).IsRequired();
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.Property(x => x.SellingPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MedicineBatch>(e =>
        {
            e.HasKey(x => x.BatchId);
            e.Property(x => x.BatchNumber).HasMaxLength(60).IsRequired();
            e.Property(x => x.Supplier).HasMaxLength(150);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.HasOne(x => x.Medicine).WithMany(m => m.Batches).HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransaction>(e =>
        {
            e.HasKey(x => x.TransactionId);
            e.Property(x => x.Notes).HasMaxLength(300);
            e.HasOne(x => x.Medicine).WithMany(m => m.Transactions).HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}


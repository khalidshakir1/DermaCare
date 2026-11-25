# 🏥 DermaCare Clinic Management System — Full Project Plan

> **Client**: Dermatologist Clinic  
> **Platform**: Windows Desktop Application  
> **Stack**: .NET 8 (WPF) + SQL Server 2022  
> **Developer**: Solo  
> **Goal**: A fully functional clinic management system covering patient records, appointments, doctor management, billing, and medical records.

---

## 1. Project Overview

DermaCare is a Windows desktop application built with **WPF (.NET 8)** using the **MVVM** pattern. It connects to a local or network **SQL Server** database. The app will be deployed as a single installer (`.exe` via NSIS or MSI via WiX).

### Key Design Principles
- **Offline-first**: The system works without internet (local SQL Server).
- **Role-based access**: Receptionist, Doctor, Admin.
- **Clean MVVM architecture**: easy to maintain and extend as a solo dev.
- **Printable reports**: Prescriptions, invoices, appointment slips.

---

## 2. Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (.NET 8) |
| Architecture | MVVM + Repository Pattern |
| ORM | Entity Framework Core 8 |
| Database | SQL Server 2022 (LocalDB for dev, full instance for production) |
| DI Container | Microsoft.Extensions.DependencyInjection |
| UI Toolkit | MaterialDesignInXaml (MahApps.Metro optional) |
| Reporting / Printing | FastReport.NET Community or Crystal Reports |
| Packaging | NSIS Installer or WiX Toolset |
| Logging | Serilog → rolling file logs |
| Unit Testing | xUnit + Moq |
| Version Control | Git (GitHub / Azure DevOps) |

---

## 3. System Modules

### 3.1 Authentication & Role Management
- Login screen with username + password (hashed with BCrypt)
- Three roles: **Admin**, **Doctor**, **Receptionist**
- Session management (in-memory, cleared on logout)
- Password reset by Admin

### 3.2 Patient Management
- Register new patient (name, DOB, gender, contact, address, skin type, photo optional)
- Search / filter patients (by name, ID, phone)
- View patient profile with full history
- Edit / deactivate patient records
- Unique Patient ID auto-generation (e.g. `DERM-2024-00001`)

### 3.3 Appointment Management
- Calendar view (day / week / month)
- Book appointment: patient → doctor → date/time → type (consultation, follow-up, procedure)
- Appointment statuses: Scheduled, Confirmed, In-Progress, Completed, Cancelled, No-Show
- SMS/email reminder toggle (manual or via SMTP)
- Walk-in quick booking
- Waiting list support

### 3.4 Doctor Management
- Add / edit doctors (name, specialization, schedule, contact)
- Weekly availability configuration (days + time slots)
- View each doctor's appointment load per day
- Doctor dashboard: today's schedule at a glance

### 3.5 Medical Records (EMR)
- Create consultation record per visit (chief complaint, diagnosis, treatment plan)
- Dermatology-specific fields: skin condition tags (ICD-10 codes), affected area map
- Prescription builder: drug name, dose, frequency, duration, instructions
- Procedure notes: Botox, chemical peel, laser, biopsy, etc.
- Upload patient photos (before/after treatment)
- Print prescription slip (branded letterhead)

### 3.6 Billing & Payments
- Generate invoice per visit (services + medicines)
- Service catalog with prices (consultation fee, procedure fee, etc.)
- Payment methods: Cash, Card, Insurance (partial)
- Invoice statuses: Draft, Issued, Paid, Partially Paid, Void
- Print/export invoice as PDF
- Daily revenue summary report

### 3.7 Reports & Analytics
- Daily/weekly/monthly appointment report
- Revenue report (by date range, by doctor, by service)
- Patient statistics (new vs. returning, age distribution)
- Export to PDF or Excel (using EPPlus for Excel)

### 3.8 Settings & Administration
- Clinic profile (name, address, logo, contact)
- User management (add/edit/deactivate users)
- Service catalog management
- Database backup & restore
- Audit log viewer (who changed what, when)

---

## 4. Database Design

### Core Tables

```sql
-- Users & Roles
Users (UserId, Username, PasswordHash, RoleId, FullName, IsActive, CreatedAt)
Roles (RoleId, RoleName)

-- Patients
Patients (PatientId, PatientCode, FirstName, LastName, DOB, Gender, 
          Phone, Email, Address, SkinType, PhotoPath, IsActive, CreatedAt)

-- Doctors
Doctors (DoctorId, UserId[FK], Specialization, Phone, IsActive)
DoctorSchedules (ScheduleId, DoctorId[FK], DayOfWeek, StartTime, EndTime)

-- Appointments
Appointments (AppointmentId, PatientId[FK], DoctorId[FK], 
              AppointmentDate, AppointmentTime, Type, Status, 
              Notes, CreatedBy, CreatedAt)

-- Medical Records
MedicalRecords (RecordId, AppointmentId[FK], PatientId[FK], DoctorId[FK],
                ChiefComplaint, Diagnosis, TreatmentPlan, ICD10Code,
                CreatedAt)
Prescriptions (PrescriptionId, RecordId[FK], DrugName, Dose, 
               Frequency, Duration, Instructions)
PatientPhotos (PhotoId, PatientId[FK], RecordId[FK], FilePath, 
               PhotoType, TakenAt)

-- Billing
Invoices (InvoiceId, PatientId[FK], AppointmentId[FK], 
          TotalAmount, PaidAmount, Status, IssuedAt, PaidAt)
InvoiceItems (ItemId, InvoiceId[FK], ServiceId[FK], Description, 
              Quantity, UnitPrice)
Services (ServiceId, ServiceName, DefaultPrice, Category, IsActive)
Payments (PaymentId, InvoiceId[FK], Amount, Method, PaidAt, ReceivedBy)

-- Audit
AuditLogs (LogId, UserId, Action, TableName, RecordId, OldValues, 
           NewValues, Timestamp)
```

---

## 5. Project File & Folder Structure

```
DermaCare/
├── DermaCare.sln
│
├── src/
│   ├── DermaCare.UI/                  ← WPF Application (startup project)
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── Views/
│   │   │   ├── Auth/
│   │   │   │   └── LoginView.xaml
│   │   │   ├── Patients/
│   │   │   │   ├── PatientListView.xaml
│   │   │   │   ├── PatientFormView.xaml
│   │   │   │   └── PatientProfileView.xaml
│   │   │   ├── Appointments/
│   │   │   │   ├── AppointmentCalendarView.xaml
│   │   │   │   └── AppointmentFormView.xaml
│   │   │   ├── Doctors/
│   │   │   │   ├── DoctorListView.xaml
│   │   │   │   └── DoctorFormView.xaml
│   │   │   ├── MedicalRecords/
│   │   │   │   ├── ConsultationView.xaml
│   │   │   │   └── PrescriptionView.xaml
│   │   │   ├── Billing/
│   │   │   │   ├── InvoiceView.xaml
│   │   │   │   └── PaymentView.xaml
│   │   │   ├── Reports/
│   │   │   │   └── ReportsView.xaml
│   │   │   └── Settings/
│   │   │       ├── SettingsView.xaml
│   │   │       └── UserManagementView.xaml
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs
│   │   │   ├── Auth/LoginViewModel.cs
│   │   │   ├── Patients/PatientListViewModel.cs
│   │   │   ├── Appointments/AppointmentViewModel.cs
│   │   │   ├── MedicalRecords/ConsultationViewModel.cs
│   │   │   └── Billing/InvoiceViewModel.cs
│   │   ├── Commands/
│   │   │   └── RelayCommand.cs
│   │   └── Helpers/
│   │       ├── NavigationService.cs
│   │       └── DialogService.cs
│   │
│   ├── DermaCare.Core/                ← Business logic, interfaces, models
│   │   ├── Models/
│   │   │   ├── Patient.cs
│   │   │   ├── Doctor.cs
│   │   │   ├── Appointment.cs
│   │   │   ├── MedicalRecord.cs
│   │   │   ├── Invoice.cs
│   │   │   └── User.cs
│   │   ├── Interfaces/
│   │   │   ├── IPatientRepository.cs
│   │   │   ├── IAppointmentRepository.cs
│   │   │   ├── IDoctorRepository.cs
│   │   │   ├── IMedicalRecordRepository.cs
│   │   │   └── IBillingRepository.cs
│   │   └── Services/
│   │       ├── PatientService.cs
│   │       ├── AppointmentService.cs
│   │       ├── BillingService.cs
│   │       └── ReportService.cs
│   │
│   ├── DermaCare.Data/                ← EF Core, Repositories, Migrations
│   │   ├── DermaDbContext.cs
│   │   ├── Repositories/
│   │   │   ├── PatientRepository.cs
│   │   │   ├── AppointmentRepository.cs
│   │   │   ├── DoctorRepository.cs
│   │   │   ├── MedicalRecordRepository.cs
│   │   │   └── BillingRepository.cs
│   │   └── Migrations/
│   │
│   └── DermaCare.Reports/             ← Report templates + PDF export
│       ├── Templates/
│       │   ├── Prescription.rdlc
│       │   └── Invoice.rdlc
│       └── ReportExporter.cs
│
├── tests/
│   ├── DermaCare.Core.Tests/
│   │   ├── PatientServiceTests.cs
│   │   ├── AppointmentServiceTests.cs
│   │   └── BillingServiceTests.cs
│   └── DermaCare.Data.Tests/
│       └── RepositoryIntegrationTests.cs
│
├── installer/
│   └── setup.iss                      ← Inno Setup script
│
└── docs/
    ├── PLAN.md                        ← This file
    ├── DATABASE_SCHEMA.md
    └── USER_MANUAL.md
```

---

## 6. Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                   WPF UI Layer (Views)                   │
│          XAML Views + Data Binding + Commands            │
└─────────────────────┬───────────────────────────────────┘
                      │  ICommand / INotifyPropertyChanged
┌─────────────────────▼───────────────────────────────────┐
│               ViewModel Layer (MVVM)                     │
│    State management, validation, UI logic                │
└─────────────────────┬───────────────────────────────────┘
                      │  Service interfaces
┌─────────────────────▼───────────────────────────────────┐
│               Core / Business Logic Layer                │
│     Services (Patient, Appointment, Billing, Report)     │
└─────────────────────┬───────────────────────────────────┘
                      │  Repository interfaces
┌─────────────────────▼───────────────────────────────────┐
│               Data Access Layer (EF Core 8)              │
│       Repositories → DermaDbContext → SQL Server         │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Development Milestones

### Milestone 1 — Project Setup & Infrastructure (Week 1)
- [ ] Create solution with 4 projects (UI, Core, Data, Reports)
- [ ] Install NuGet packages (EF Core, MaterialDesign, Serilog, BCrypt, EPPlus)
- [ ] Set up `DermaDbContext` and all entity models
- [ ] Create initial EF Core migrations and seed data (roles, admin user, services)
- [ ] Configure DI container in `App.xaml.cs`
- [ ] Build `BaseViewModel` with `INotifyPropertyChanged` and `RelayCommand`
- [ ] Build `NavigationService` for switching views
- [ ] Design `MainWindow` shell (sidebar nav + content frame)

**✅ Verification**: App launches, navigates between empty views, database tables created.

---

### Milestone 2 — Authentication (Week 1–2)
- [ ] Login screen UI
- [ ] `LoginViewModel` with credential validation
- [ ] BCrypt password hashing on User model
- [ ] Session singleton (`CurrentSession.cs`) storing logged-in user + role
- [ ] Role-based nav menu visibility (hide admin items from receptionist)
- [ ] Logout functionality

**✅ Verification**: Login with correct credentials → dashboard. Wrong password → error message. Receptionist cannot see Admin menu items.

---

### Milestone 3 — Patient Management (Week 2–3)
- [ ] Patient list view with DataGrid (search, filter, sort)
- [ ] Add/Edit patient form with validation
- [ ] Auto-generate `PatientCode`
- [ ] Photo upload (optional) with file picker
- [ ] Patient profile view with appointment + billing history tabs
- [ ] Deactivate patient (soft delete)

**✅ Verification**: CRUD operations work. Search returns correct patients. PatientCode is unique. Deactivated patients hidden from default list.

---

### Milestone 4 — Doctor Management (Week 3)
- [ ] Doctor list and form
- [ ] Link doctor to a system User account
- [ ] Configure weekly availability (day + time slots)
- [ ] Doctor dashboard widget (today's appointments count)

**✅ Verification**: Doctors appear in appointment booking. Schedule blocks unavailable slots.

---

### Milestone 5 — Appointment Management (Week 3–4)
- [ ] Calendar view (use `Telerik` free or build a simple day-grid with `ItemsControl`)
- [ ] Appointment booking form (patient search, doctor, slot picker)
- [ ] Walk-in quick booking button
- [ ] Status change workflow (Scheduled → Confirmed → In-Progress → Completed)
- [ ] Appointment cancellation with reason
- [ ] Conflict detection (double-booking prevention)

**✅ Verification**: Cannot book two patients at the same doctor/time. Status transitions work. Calendar updates in real-time.

---

### Milestone 6 — Medical Records / EMR (Week 4–5)
- [ ] Consultation form (opens from completed appointment)
- [ ] Chief complaint, diagnosis, ICD-10 code (dropdown/autocomplete)
- [ ] Prescription builder (add drug rows dynamically)
- [ ] Procedure notes text area
- [ ] Before/after photo upload linked to record
- [ ] Print prescription (RDLC report or FastReport template)

**✅ Verification**: Medical record saved and shows in patient profile history. Prescription prints with clinic header, patient name, doctor signature line.

---

### Milestone 7 — Billing & Invoicing (Week 5–6)
- [ ] Service catalog management (admin)
- [ ] Invoice generation (auto-populated from appointment services)
- [ ] Add line items manually
- [ ] Record payment (cash/card) with change calculation
- [ ] Partial payment / outstanding balance tracking
- [ ] Print/export invoice as PDF
- [ ] Daily revenue report view

**✅ Verification**: Invoice totals match. Paid invoice marked correctly. PDF opens correctly with all items listed.

---

### Milestone 8 — Reports & Export (Week 6)
- [ ] Appointment report (date range, doctor filter)
- [ ] Revenue report (by date range, by doctor)
- [ ] New vs. returning patient chart (use `LiveCharts2` WPF)
- [ ] Export to Excel using EPPlus
- [ ] Export to PDF

**✅ Verification**: Reports show correct numbers. Excel export opens in MS Excel without errors.

---

### Milestone 9 — Settings & Administration (Week 7)
- [ ] Clinic profile editor (name, address, logo)
- [ ] User management (add/edit/deactivate users, assign roles)
- [ ] Service catalog management
- [ ] Database backup (copy `.mdf` / use SQL Server backup command)
- [ ] Audit log viewer (filterable table)

**✅ Verification**: New user can log in with assigned role. Backup file created at specified path.

---

### Milestone 10 — Polish, Testing & Installer (Week 7–8)
- [ ] Write xUnit tests for all service methods
- [ ] Integration tests for critical repositories
- [ ] Error handling + user-friendly error dialogs
- [ ] Loading spinners on slow operations
- [ ] Keyboard shortcuts (F2 = new patient, F3 = new appointment, etc.)
- [ ] Dark/Light theme toggle
- [ ] Build Inno Setup installer (`setup.iss`) bundling app + SQL Server LocalDB
- [ ] Create `USER_MANUAL.md`
- [ ] Final UAT (User Acceptance Testing) with client

**✅ Verification**: Installer runs on a clean Windows 10/11 VM. All major workflows complete without crashes. Test coverage ≥ 70%.

---

## 8. NuGet Package List

```xml
<!-- DermaCare.UI -->
<PackageReference Include="MaterialDesignThemes" Version="5.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
<PackageReference Include="LiveChartsCore.SkiaSharpView.WPF" Version="2.*" />

<!-- DermaCare.Core -->
<PackageReference Include="BCrypt.Net-Next" Version="4.*" />
<PackageReference Include="FluentValidation" Version="11.*" />

<!-- DermaCare.Data -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />

<!-- DermaCare.Reports -->
<PackageReference Include="EPPlus" Version="7.*" />
<PackageReference Include="PdfSharp-MigraDoc" Version="6.*" />
```

---

## 9. Estimated Timeline

| Week | Focus |
|------|-------|
| Week 1 | Setup, Infrastructure, Auth |
| Week 2 | Patient Management |
| Week 3 | Doctor + Appointment Management |
| Week 4 | Appointment (calendar) + EMR |
| Week 5 | Billing & Invoicing |
| Week 6 | Reports & Export |
| Week 7 | Settings, Admin, Testing |
| Week 8 | Polish, Installer, UAT |

**Total estimated effort**: ~8 weeks solo developer (part-time ~4–5 hrs/day)

---

## 10. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Calendar complexity (WPF has no built-in calendar grid) | Use a simple day-view with `ItemsControl` or Telerik's free calendar |
| SQL Server licensing on client machine | Use SQL Server Express (free) or LocalDB for small clinic |
| Print/PDF quality | Use RDLC (built-in .NET) or FastReport Community edition |
| Solo dev scope creep | Freeze scope at Milestone 5; phase 2 for extra features |
| Data loss | Daily auto-backup to a secondary folder; remind client to back up externally |

---

## 11. Deliverables

1. ✅ Full source code (Git repo)
2. ✅ Windows installer (`.exe`)
3. ✅ SQL Server setup script (`schema.sql`)
4. ✅ User manual (`USER_MANUAL.md`)
5. ✅ 1-hour training session for clinic staff

---

> **Next Step for Gemini**: Switch model to Gemini and reference this `PLAN.md`. Ask it to start with **Milestone 1** — create the solution, projects, database context, and DI setup.

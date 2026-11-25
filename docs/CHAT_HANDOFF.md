# DermaCare — Chat Handoff & Context Summary

> Use this file in the Windows VM by opening `C:\DermaCare` in Antigravity and referencing `@docs/CHAT_HANDOFF.md` and `@docs/PLAN.md`.

## 1. Project Decisions
- **Client**: Dermatologist Clinic
- **Platform**: Windows Desktop Application (.NET 8 WPF)
- **Database**: SQL Server 2022 / LocalDB (`Server=(localdb)\mssqllocaldb;Database=DermaCareDb`)
- **Architecture**: MVVM + Repository Pattern + EF Core 8 + DI (`Microsoft.Extensions.DependencyInjection`)
- **UI**: `MaterialDesignThemes` (Light/Dark theme toggle)
- **Default Admin Login**: `admin` / `Admin@123`

## 2. What Has Been Built (All 10 Milestones)
1. **Milestone 1 (Setup & Infrastructure)**: `DermaCare.sln` with 5 projects (`DermaCare.UI`, `DermaCare.Core`, `DermaCare.Data`, `DermaCare.Reports`, `DermaCare.Core.Tests`), `DermaDbContext`, `DermaDbContextFactory`, `DatabaseSeeder`, DI container, `MainWindow` shell, `NavigationService`.
2. **Milestone 2 (Auth & Roles)**: `AuthService`, `CurrentSession`, BCrypt password verification, `LoginView` / `LoginViewModel`, role-based sidebar visibility (`Admin`, `Doctor`, `Receptionist`), Sign Out.
3. **Milestone 3 (Patients)**: `PatientService`, `PatientListView`, `PatientFormView` (with Fitzpatrick skin type + photo picker), `PatientProfileView` (Appointments, Medical Records, Billing tabs), soft-delete.
4. **Milestone 4 (Doctors)**: `DoctorService`, `DoctorListView`, `DoctorFormView` (user account linking + weekly availability slots), Dashboard **Today's Appointments** card.
5. **Milestone 5 (Appointments)**: `AppointmentService` (double-booking conflict detection + weekly schedule check), `AppointmentCalendarView` (date/doctor filters, Walk-In, Confirm/Start/Complete/Cancel), `AppointmentFormView`.
6. **Milestone 6 (EMR / Consultations)**: `MedicalRecordService`, `ConsultationView` (ICD-10 presets, dynamic prescription builder, photo attachment, **Open Consultation** from Appointments), `PdfReportExporter.ExportPrescription`.
7. **Milestone 7 (Billing & Invoicing)**: `BillingService`, `InvoiceView` (create invoice from service catalog, record Cash/Card/Insurance payments, `PdfReportExporter.ExportInvoice`).
8. **Milestone 8 (Reports & Analytics)**: `ReportService`, `ReportsView` (date-range summary cards + appointments/invoices tables), `ExcelReportExporter` (EPPlus `.xlsx`).
9. **Milestone 9 (Settings & Admin)**: `AdminService`, `SettingsView` (Clinic Profile, User Management, Service Catalog, Audit Logs, SQL Server Backup).
10. **Milestone 10 (Polish & Installer)**: Light/Dark theme toggle, `F2` (New Patient) & `F3` (Book Appointment) shortcuts, `installer/setup.iss` (Inno Setup), `docs/USER_MANUAL.md`.

## 3. What Remains to Run on Windows VM
Because the project was authored on macOS (without `.NET 8 SDK` or Windows WPF runtime), the following commands still need to be run inside the Windows VM:

```powershell
cd C:\DermaCare
dotnet tool install --global dotnet-ef
dotnet restore
dotnet ef migrations add InitialCreate --project src/DermaCare.Data --startup-project src/DermaCare.UI
dotnet build
dotnet test
dotnet run --project src/DermaCare.UI
```

To package the final client installer (`DermaCareSetup.exe`):

```powershell
dotnet publish src/DermaCare.UI/DermaCare.UI.csproj -c Release -r win-x64 --self-contained true
```
Then compile `installer\setup.iss` in **Inno Setup** → `installer\output\DermaCareSetup.exe`.

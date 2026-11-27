using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.Reports;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels.Appointments;
using Microsoft.Win32;

namespace DermaCare.UI.ViewModels.MedicalRecords;

public class ConsultationViewModel : BaseViewModel
{
    private readonly IMedicalRecordService _recordService;
    private readonly IBillingService _billingService;
    private readonly INavigationService _navigationService;
    private readonly PdfReportExporter _pdfExporter;

    private Appointment? _appointment;
    private string _chiefComplaint = string.Empty;
    private string _diagnosis = string.Empty;
    private string? _icd10Code;
    private string _treatmentPlan = string.Empty;
    private string? _procedureNotes;

    private string _drugName = string.Empty;
    private string _dose = string.Empty;
    private string _frequency = "Twice daily";
    private string _duration = "7 days";
    private string? _instructions;
    private string? _statusMessage;

    public ConsultationViewModel(
        IMedicalRecordService recordService,
        IBillingService billingService,
        INavigationService navigationService,
        PdfReportExporter pdfExporter)
    {
        _recordService = recordService;
        _billingService = billingService;
        _navigationService = navigationService;
        _pdfExporter = pdfExporter;

        Prescriptions = new ObservableCollection<Prescription>();
        Photos = new ObservableCollection<PatientPhoto>();
        CommonIcd10Codes = new[]
        {
            "L70.0 - Acne vulgaris",
            "L20.9 - Atopic dermatitis",
            "L40.0 - Psoriasis vulgaris",
            "L30.9 - Dermatitis, unspecified",
            "B35.4 - Tinea corporis",
            "C44.9 - Skin neoplasm"
        };

        AddPrescriptionCommand = new RelayCommand(AddPrescription);
        RemovePrescriptionCommand = new RelayCommand(p =>
        {
            if (p is Prescription rx) Prescriptions.Remove(rx);
        });
        AttachPhotoCommand = new RelayCommand(AttachPhoto);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        PrintPrescriptionCommand = new RelayCommand(async () => await PrintPrescriptionAsync(), () => _appointment?.Patient is not null);
        ExportPdfCommand = new RelayCommand(ExportPrescriptionPdf, () => _appointment?.Patient is not null);
        BackCommand = new RelayCommand(() => _navigationService.NavigateTo<AppointmentCalendarViewModel>());
    }

    public ObservableCollection<Prescription> Prescriptions { get; }
    public ObservableCollection<PatientPhoto> Photos { get; }
    public IReadOnlyList<string> CommonIcd10Codes { get; }

    public string HeaderTitle => _appointment is null
        ? "Consultation"
        : $"Consultation — {_appointment.Patient?.FullName} ({_appointment.AppointmentDate:d})";

    public string ChiefComplaint { get => _chiefComplaint; set => SetProperty(ref _chiefComplaint, value); }
    public string Diagnosis { get => _diagnosis; set => SetProperty(ref _diagnosis, value); }
    public string? Icd10Code { get => _icd10Code; set => SetProperty(ref _icd10Code, value); }
    public string TreatmentPlan { get => _treatmentPlan; set => SetProperty(ref _treatmentPlan, value); }
    public string? ProcedureNotes { get => _procedureNotes; set => SetProperty(ref _procedureNotes, value); }

    public string DrugName { get => _drugName; set => SetProperty(ref _drugName, value); }
    public string Dose { get => _dose; set => SetProperty(ref _dose, value); }
    public string Frequency { get => _frequency; set => SetProperty(ref _frequency, value); }
    public string Duration { get => _duration; set => SetProperty(ref _duration, value); }
    public string? Instructions { get => _instructions; set => SetProperty(ref _instructions, value); }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand AddPrescriptionCommand { get; }
    public ICommand RemovePrescriptionCommand { get; }
    public ICommand AttachPhotoCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand PrintPrescriptionCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand BackCommand { get; }

    public void Initialize(Appointment appointment)
    {
        _appointment = appointment;
        OnPropertyChanged(nameof(HeaderTitle));
    }

    private void AddPrescription()
    {
        if (string.IsNullOrWhiteSpace(DrugName) || string.IsNullOrWhiteSpace(Dose))
        {
            StatusMessage = "Drug name and dose are required.";
            return;
        }

        Prescriptions.Add(new Prescription
        {
            DrugName = DrugName.Trim(),
            Dose = Dose.Trim(),
            Frequency = Frequency.Trim(),
            Duration = Duration.Trim(),
            Instructions = Instructions?.Trim()
        });

        DrugName = string.Empty;
        Dose = string.Empty;
        Instructions = null;
        StatusMessage = null;
    }

    private void AttachPhoto()
    {
        if (_appointment is null) return;
        var dialog = new OpenFileDialog
        {
            Title = "Attach Clinical Photo",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == true)
        {
            Photos.Add(new PatientPhoto
            {
                PatientId = _appointment.PatientId ?? 0,
                FilePath = dialog.FileName,
                PhotoType = PhotoType.Before,
                TakenAt = DateTime.UtcNow
            });
        }
    }

    private async Task SaveAsync()
    {
        if (_appointment is null) return;

        var record = new MedicalRecord
        {
            AppointmentId = _appointment.AppointmentId,
            PatientId = _appointment.PatientId ?? 0,
            DoctorId = _appointment.DoctorId,
            ChiefComplaint = ChiefComplaint,
            Diagnosis = Diagnosis,
            ICD10Code = Icd10Code?.Split(' ')[0],
            TreatmentPlan = TreatmentPlan,
            ProcedureNotes = ProcedureNotes,
            Prescriptions = Prescriptions.ToList(),
            Photos = Photos.ToList()
        };

        var result = await _recordService.SaveConsultationAsync(record);
        StatusMessage = result.Success ? "Consultation saved successfully." : result.ErrorMessage;
    }

    private void ExportPrescriptionPdf()
    {
        if (_appointment?.Patient is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Save Prescription PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"Prescription_{_appointment.Patient.PatientCode}_{DateTime.Today:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog() != true) return;

        var record = new MedicalRecord
        {
            Diagnosis = Diagnosis,
            ICD10Code = Icd10Code?.Split(' ')[0],
            Doctor = _appointment.Doctor,
            Prescriptions = Prescriptions.ToList(),
            CreatedAt = DateTime.Now
        };

        _pdfExporter.ExportPrescription(record, _appointment.Patient, new ClinicProfile(), dialog.FileName);
        StatusMessage = $"Prescription exported to {dialog.FileName}";
    }

    private async Task PrintPrescriptionAsync()
    {
        if (_appointment?.Patient is null) return;
        var clinic = await _billingService.GetClinicProfileAsync();
        var record = new MedicalRecord
        {
            Diagnosis = Diagnosis,
            ICD10Code = Icd10Code?.Split(' ')[0],
            Doctor = _appointment.Doctor,
            Prescriptions = Prescriptions.ToList(),
            CreatedAt = DateTime.Now
        };

        if (ReceiptPrintHelper.PrintPrescription(record, _appointment.Patient, clinic))
            StatusMessage = "Prescription sent to printer.";
    }
}

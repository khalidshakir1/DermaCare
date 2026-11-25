using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Patients;

public class PatientProfileViewModel : BaseViewModel
{
    private readonly IPatientService _patientService;
    private readonly INavigationService _navigationService;
    private Patient? _patient;

    public PatientProfileViewModel(IPatientService patientService, INavigationService navigationService)
    {
        _patientService = patientService;
        _navigationService = navigationService;

        Appointments = new ObservableCollection<Appointment>();
        MedicalRecords = new ObservableCollection<MedicalRecord>();
        Invoices = new ObservableCollection<Invoice>();

        BackCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientListViewModel>());
        EditCommand = new RelayCommand(
            () => _navigationService.NavigateTo<PatientFormViewModel>(vm => vm.Initialize(Patient)),
            () => Patient is not null);
    }

    public Patient? Patient
    {
        get => _patient;
        private set => SetProperty(ref _patient, value);
    }

    public ObservableCollection<Appointment> Appointments { get; }
    public ObservableCollection<MedicalRecord> MedicalRecords { get; }
    public ObservableCollection<Invoice> Invoices { get; }

    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }

    public async Task LoadAsync(int patientId)
    {
        Patient = await _patientService.GetByIdAsync(patientId);
        Appointments.Clear();
        MedicalRecords.Clear();
        Invoices.Clear();

        if (Patient is null) return;

        foreach (var a in Patient.Appointments.OrderByDescending(x => x.AppointmentDate))
            Appointments.Add(a);
        foreach (var m in Patient.MedicalRecords.OrderByDescending(x => x.CreatedAt))
            MedicalRecords.Add(m);
        foreach (var i in Patient.Invoices.OrderByDescending(x => x.IssuedAt))
            Invoices.Add(i);
    }
}

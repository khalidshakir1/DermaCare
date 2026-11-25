using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Appointments;

public class AppointmentFormViewModel : BaseViewModel
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly INavigationService _navigationService;

    private Patient? _selectedPatient;
    private Doctor? _selectedDoctor;
    private DateTime _appointmentDate = DateTime.Today;
    private string _appointmentTime = "09:00";
    private AppointmentType _type = AppointmentType.Consultation;
    private string? _notes;
    private string? _errorMessage;

    public AppointmentFormViewModel(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService,
        INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
        _navigationService = navigationService;

        Patients = new ObservableCollection<Patient>();
        Doctors = new ObservableCollection<Doctor>();
        AppointmentTypes = Enum.GetValues<AppointmentType>();

        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<AppointmentCalendarViewModel>());
    }

    public ObservableCollection<Patient> Patients { get; }
    public ObservableCollection<Doctor> Doctors { get; }
    public IReadOnlyList<AppointmentType> AppointmentTypes { get; }

    public Patient? SelectedPatient { get => _selectedPatient; set => SetProperty(ref _selectedPatient, value); }
    public Doctor? SelectedDoctor { get => _selectedDoctor; set => SetProperty(ref _selectedDoctor, value); }
    public DateTime AppointmentDate { get => _appointmentDate; set => SetProperty(ref _appointmentDate, value); }
    public string AppointmentTime { get => _appointmentTime; set => SetProperty(ref _appointmentTime, value); }
    public AppointmentType Type { get => _type; set => SetProperty(ref _type, value); }
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeAsync(bool isWalkIn, DateTime date)
    {
        AppointmentDate = date;
        Type = isWalkIn ? AppointmentType.WalkIn : AppointmentType.Consultation;
        if (isWalkIn)
            AppointmentTime = DateTime.Now.ToString("HH:mm");

        var patients = await _patientService.SearchAsync(null);
        Patients.Clear();
        foreach (var p in patients)
            Patients.Add(p);

        var doctors = await _doctorService.GetAllActiveAsync();
        Doctors.Clear();
        foreach (var d in doctors)
            Doctors.Add(d);

        SelectedPatient = Patients.FirstOrDefault();
        SelectedDoctor = Doctors.FirstOrDefault();
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedPatient is null || SelectedDoctor is null)
        {
            ErrorMessage = "Please select both a patient and a doctor.";
            return;
        }

        if (!TimeSpan.TryParse(AppointmentTime, out var time))
        {
            ErrorMessage = "Enter time in HH:mm format (e.g. 10:30).";
            return;
        }

        var appointment = new Appointment
        {
            PatientId = SelectedPatient.PatientId,
            DoctorId = SelectedDoctor.DoctorId,
            AppointmentDate = AppointmentDate,
            AppointmentTime = time,
            Type = Type,
            Notes = Notes
        };

        var result = await _appointmentService.BookAsync(appointment);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        _navigationService.NavigateTo<AppointmentCalendarViewModel>();
    }
}

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
    private readonly IDoctorService _doctorService;
    private readonly IBillingService _billingService;
    private readonly INavigationService _navigationService;

    private Doctor? _clinicDoctor;
    private string _patientName = string.Empty;
    private string _patientPhone = string.Empty;
    private ServiceItem? _selectedService;
    private DateTime _appointmentDate = DateTime.Today;
    private string _appointmentTime = "09:00";
    private AppointmentType _type = AppointmentType.Consultation;
    private string? _notes;
    private bool _printTokenOnSave = true;
    private string? _errorMessage;

    public AppointmentFormViewModel(
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        IBillingService billingService,
        INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _billingService = billingService;
        _navigationService = navigationService;

        Services = new ObservableCollection<ServiceItem>();
        AppointmentTypes = Enum.GetValues<AppointmentType>()
            .Where(t => t != AppointmentType.WalkIn)
            .ToList();

        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<AppointmentCalendarViewModel>());
    }

    public ObservableCollection<ServiceItem> Services { get; }
    public IReadOnlyList<AppointmentType> AppointmentTypes { get; }

    public string PatientName { get => _patientName; set => SetProperty(ref _patientName, value); }
    public string PatientPhone { get => _patientPhone; set => SetProperty(ref _patientPhone, value); }
    public ServiceItem? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }
    public DateTime AppointmentDate { get => _appointmentDate; set => SetProperty(ref _appointmentDate, value); }
    public string AppointmentTime { get => _appointmentTime; set => SetProperty(ref _appointmentTime, value); }
    public AppointmentType Type { get => _type; set => SetProperty(ref _type, value); }
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public bool PrintTokenOnSave { get => _printTokenOnSave; set => SetProperty(ref _printTokenOnSave, value); }

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

        var doctors = await _doctorService.GetAllActiveAsync();
        _clinicDoctor = doctors.FirstOrDefault();

        var services = await _billingService.GetActiveServicesAsync();
        Services.Clear();
        foreach (var s in services)
            Services.Add(s);
        SelectedService = Services.FirstOrDefault();
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (Type != AppointmentType.WalkIn)
        {
            if (string.IsNullOrWhiteSpace(PatientName))
            {
                ErrorMessage = "Please enter patient name.";
                return;
            }
            if (string.IsNullOrWhiteSpace(PatientPhone))
            {
                ErrorMessage = "Please enter patient phone number.";
                return;
            }
        }

        if (!TimeSpan.TryParse(AppointmentTime, out var time))
        {
            ErrorMessage = "Enter time in HH:mm format (e.g. 10:30).";
            return;
        }

        var combinedNotes = Notes;
        if (SelectedService is not null)
        {
            combinedNotes = string.IsNullOrWhiteSpace(Notes)
                ? SelectedService.ServiceName
                : $"{SelectedService.ServiceName} — {Notes.Trim()}";
        }

        var appointment = new Appointment
        {
            PatientName = PatientName,
            PatientPhone = PatientPhone,
            DoctorId = _clinicDoctor?.DoctorId ?? 0,
            AppointmentDate = AppointmentDate,
            AppointmentTime = time,
            Type = Type,
            Notes = combinedNotes
        };

        var result = await _appointmentService.BookAsync(appointment);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        if (PrintTokenOnSave && result.Data is not null)
        {
            result.Data.Doctor = _clinicDoctor;
            var clinic = await _billingService.GetClinicProfileAsync();
            ReceiptPrintHelper.PrintThermalToken(result.Data, clinic);
        }

        _navigationService.NavigateTo<AppointmentCalendarViewModel>();
    }
}

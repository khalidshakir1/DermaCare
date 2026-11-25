using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels.Billing;
using DermaCare.UI.ViewModels.MedicalRecords;

namespace DermaCare.UI.ViewModels.Appointments;

public class AppointmentCalendarViewModel : BaseViewModel
{
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly INavigationService _navigationService;

    private DateTime _selectedDate = DateTime.Today;
    private Doctor? _selectedDoctorFilter;
    private Appointment? _selectedAppointment;
    private string _cancellationReason = string.Empty;

    public AppointmentCalendarViewModel(
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _navigationService = navigationService;

        Appointments = new ObservableCollection<Appointment>();
        Doctors = new ObservableCollection<Doctor>();

        RefreshCommand = new RelayCommand(async () => await LoadAppointmentsAsync());
        NewAppointmentCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(isWalkIn: false, SelectedDate)));
        WalkInCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<AppointmentFormViewModel>(vm => _ = vm.InitializeAsync(isWalkIn: true, DateTime.Today)));
        ConfirmCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Confirmed),
            () => SelectedAppointment is { Status: AppointmentStatus.Scheduled });
        StartVisitCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.InProgress),
            () => SelectedAppointment is { Status: AppointmentStatus.Scheduled or AppointmentStatus.Confirmed });
        CompleteCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Completed),
            () => SelectedAppointment is { Status: AppointmentStatus.InProgress });
        OpenConsultationCommand = new RelayCommand(
            () => _navigationService.NavigateTo<ConsultationViewModel>(vm => vm.Initialize(SelectedAppointment!)),
            () => SelectedAppointment is { Status: AppointmentStatus.InProgress or AppointmentStatus.Completed });
        GenerateBillCommand = new RelayCommand(
            () => _navigationService.NavigateTo<InvoiceViewModel>(vm => vm.PreselectPatient(SelectedAppointment!.PatientId)),
            () => SelectedAppointment is not null);
        CancelAppointmentCommand = new RelayCommand(
            async () => await ChangeStatusAsync(AppointmentStatus.Cancelled, CancellationReason),
            () => SelectedAppointment is not null && SelectedAppointment.Status != AppointmentStatus.Cancelled);

        _ = InitializeAsync();
    }

    public ObservableCollection<Appointment> Appointments { get; }
    public ObservableCollection<Doctor> Doctors { get; }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
                _ = LoadAppointmentsAsync();
        }
    }

    public Doctor? SelectedDoctorFilter
    {
        get => _selectedDoctorFilter;
        set
        {
            if (SetProperty(ref _selectedDoctorFilter, value))
                _ = LoadAppointmentsAsync();
        }
    }

    public Appointment? SelectedAppointment
    {
        get => _selectedAppointment;
        set => SetProperty(ref _selectedAppointment, value);
    }

    public string CancellationReason
    {
        get => _cancellationReason;
        set => SetProperty(ref _cancellationReason, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand NewAppointmentCommand { get; }
    public ICommand WalkInCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand StartVisitCommand { get; }
    public ICommand CompleteCommand { get; }
    public ICommand OpenConsultationCommand { get; }
    public ICommand GenerateBillCommand { get; }
    public ICommand CancelAppointmentCommand { get; }

    private async Task InitializeAsync()
    {
        var docs = await _doctorService.GetAllActiveAsync();
        Doctors.Clear();
        foreach (var d in docs)
            Doctors.Add(d);

        await LoadAppointmentsAsync();
    }

    public async Task LoadAppointmentsAsync()
    {
        var items = await _appointmentService.GetByDateAsync(SelectedDate, SelectedDoctorFilter?.DoctorId);
        Appointments.Clear();
        foreach (var a in items)
            Appointments.Add(a);
    }

    private async Task ChangeStatusAsync(AppointmentStatus status, string? reason = null)
    {
        if (SelectedAppointment is null) return;
        await _appointmentService.UpdateStatusAsync(SelectedAppointment.AppointmentId, status, reason);
        CancellationReason = string.Empty;
        await LoadAppointmentsAsync();
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Doctors;

public class DoctorFormViewModel : BaseViewModel
{
    private readonly IDoctorService _doctorService;
    private readonly INavigationService _navigationService;

    private Doctor? _editingDoctor;
    private User? _selectedUser;
    private string _specialization = "Dermatology";
    private string _phone = string.Empty;
    private string? _pmdcNumber;
    private DayOfWeek _newDay = DayOfWeek.Monday;
    private string _newStartTime = "09:00";
    private string _newEndTime = "17:00";
    private string? _errorMessage;

    public DoctorFormViewModel(IDoctorService doctorService, INavigationService navigationService)
    {
        _doctorService = doctorService;
        _navigationService = navigationService;

        DoctorUsers = new ObservableCollection<User>();
        Schedules = new ObservableCollection<DoctorSchedule>();
        DaysOfWeek = Enum.GetValues<DayOfWeek>();

        AddScheduleCommand = new RelayCommand(AddSchedule);
        RemoveScheduleCommand = new RelayCommand(s =>
        {
            if (s is DoctorSchedule slot)
                Schedules.Remove(slot);
        });
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<DoctorListViewModel>());
    }

    public ObservableCollection<User> DoctorUsers { get; }
    public ObservableCollection<DoctorSchedule> Schedules { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }

    public string FormTitle => _editingDoctor is null ? "Add Doctor" : $"Edit Doctor ({_editingDoctor.User?.FullName})";

    public User? SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }
    public string Specialization { get => _specialization; set => SetProperty(ref _specialization, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string? PMDCNumber { get => _pmdcNumber; set => SetProperty(ref _pmdcNumber, value); }
    public DayOfWeek NewDay { get => _newDay; set => SetProperty(ref _newDay, value); }
    public string NewStartTime { get => _newStartTime; set => SetProperty(ref _newStartTime, value); }
    public string NewEndTime { get => _newEndTime; set => SetProperty(ref _newEndTime, value); }

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

    public ICommand AddScheduleCommand { get; }
    public ICommand RemoveScheduleCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeAsync(Doctor? existing)
    {
        _editingDoctor = existing;
        OnPropertyChanged(nameof(FormTitle));

        var users = await _doctorService.GetDoctorUsersAsync();
        DoctorUsers.Clear();
        foreach (var u in users)
            DoctorUsers.Add(u);

        Schedules.Clear();

        if (existing is null)
        {
            SelectedUser = DoctorUsers.FirstOrDefault();
            return;
        }

        SelectedUser = DoctorUsers.FirstOrDefault(u => u.UserId == existing.UserId);
        Specialization = existing.Specialization;
        Phone = existing.Phone;
        PMDCNumber = existing.PMDCNumber;
        foreach (var s in existing.Schedules)
            Schedules.Add(new DoctorSchedule
            {
                ScheduleId = s.ScheduleId,
                DoctorId = s.DoctorId,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            });
    }

    private void AddSchedule()
    {
        ErrorMessage = null;
        if (!TimeSpan.TryParse(NewStartTime, out var start) || !TimeSpan.TryParse(NewEndTime, out var end))
        {
            ErrorMessage = "Enter valid times in HH:mm format (e.g. 09:00 and 17:00).";
            return;
        }

        if (start >= end)
        {
            ErrorMessage = "Start time must be earlier than end time.";
            return;
        }

        Schedules.Add(new DoctorSchedule
        {
            DayOfWeek = NewDay,
            StartTime = start,
            EndTime = end
        });
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedUser is null)
        {
            ErrorMessage = "Select a user account with the Doctor role.";
            return;
        }

        if (_editingDoctor is null)
        {
            var doctor = new Doctor
            {
                UserId = SelectedUser.UserId,
                Specialization = Specialization,
                Phone = Phone,
                PMDCNumber = PMDCNumber,
                Schedules = Schedules.ToList()
            };

            var result = await _doctorService.CreateAsync(doctor);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
        }
        else
        {
            _editingDoctor.UserId = SelectedUser.UserId;
            _editingDoctor.Specialization = Specialization;
            _editingDoctor.Phone = Phone;
            _editingDoctor.PMDCNumber = PMDCNumber;
            _editingDoctor.Schedules.Clear();
            foreach (var s in Schedules)
                _editingDoctor.Schedules.Add(s);

            var result = await _doctorService.UpdateAsync(_editingDoctor);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
        }

        _navigationService.NavigateTo<DoctorListViewModel>();
    }
}



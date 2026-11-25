using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;
using Microsoft.Win32;

namespace DermaCare.UI.ViewModels.Patients;

public class PatientFormViewModel : BaseViewModel
{
    private readonly IPatientService _patientService;
    private readonly INavigationService _navigationService;

    private Patient? _editingPatient;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private DateTime _dateOfBirth = DateTime.Today.AddYears(-25);
    private string _gender = "Female";
    private string _phone = string.Empty;
    private string? _cnic;
    private string? _email;
    private string? _address;
    private string? _skinType;
    private string? _photoPath;
    private string? _errorMessage;

    public PatientFormViewModel(IPatientService patientService, INavigationService navigationService)
    {
        _patientService = patientService;
        _navigationService = navigationService;

        Genders = new[] { "Female", "Male", "Other" };
        SkinTypes = new[] { "Type I", "Type II", "Type III", "Type IV", "Type V", "Type VI" };

        BrowsePhotoCommand = new RelayCommand(BrowsePhoto);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientListViewModel>());
    }

    public IReadOnlyList<string> Genders { get; }
    public IReadOnlyList<string> SkinTypes { get; }

    public string FormTitle => _editingPatient is null ? "Register New Patient" : $"Edit Patient ({_editingPatient.PatientCode})";

    public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }
    public string LastName { get => _lastName; set => SetProperty(ref _lastName, value); }
    public DateTime DateOfBirth { get => _dateOfBirth; set => SetProperty(ref _dateOfBirth, value); }
    public string Gender { get => _gender; set => SetProperty(ref _gender, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string? CNIC { get => _cnic; set => SetProperty(ref _cnic, value); }
    public string? Email { get => _email; set => SetProperty(ref _email, value); }
    public string? Address { get => _address; set => SetProperty(ref _address, value); }
    public string? SkinType { get => _skinType; set => SetProperty(ref _skinType, value); }
    public string? PhotoPath { get => _photoPath; set => SetProperty(ref _photoPath, value); }

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

    public ICommand BrowsePhotoCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public void Initialize(Patient? existing)
    {
        _editingPatient = existing;
        OnPropertyChanged(nameof(FormTitle));

        if (existing is null) return;

        FirstName = existing.FirstName;
        LastName = existing.LastName;
        DateOfBirth = existing.DateOfBirth;
        Gender = existing.Gender;
        Phone = existing.Phone;
        CNIC = existing.CNIC;
        Email = existing.Email;
        Address = existing.Address;
        SkinType = existing.SkinType;
        PhotoPath = existing.PhotoPath;
    }

    private void BrowsePhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Patient Photo",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == true)
            PhotoPath = dialog.FileName;
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (_editingPatient is null)
        {
            var patient = new Patient
            {
                FirstName = FirstName,
                LastName = LastName,
                DateOfBirth = DateOfBirth,
                Gender = Gender,
                Phone = Phone,
                CNIC = CNIC,
                Email = Email,
                Address = Address,
                SkinType = SkinType,
                PhotoPath = PhotoPath
            };

            var result = await _patientService.CreateAsync(patient);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
        }
        else
        {
            _editingPatient.FirstName = FirstName;
            _editingPatient.LastName = LastName;
            _editingPatient.DateOfBirth = DateOfBirth;
            _editingPatient.Gender = Gender;
            _editingPatient.Phone = Phone;
            _editingPatient.CNIC = CNIC;
            _editingPatient.Email = Email;
            _editingPatient.Address = Address;
            _editingPatient.SkinType = SkinType;
            _editingPatient.PhotoPath = PhotoPath;

            var result = await _patientService.UpdateAsync(_editingPatient);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
        }

        _navigationService.NavigateTo<PatientListViewModel>();
    }
}



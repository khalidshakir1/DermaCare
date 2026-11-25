using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Patients;

public class PatientListViewModel : BaseViewModel
{
    private readonly IPatientService _patientService;
    private readonly INavigationService _navigationService;

    private string _searchQuery = string.Empty;
    private bool _showInactive;
    private Patient? _selectedPatient;
    private bool _isBusy;

    public PatientListViewModel(IPatientService patientService, INavigationService navigationService)
    {
        _patientService = patientService;
        _navigationService = navigationService;

        Patients = new ObservableCollection<Patient>();
        SearchCommand = new RelayCommand(async () => await LoadPatientsAsync());
        NewPatientCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<PatientFormViewModel>(vm => vm.Initialize(null)));
        EditPatientCommand = new RelayCommand(
            () => _navigationService.NavigateTo<PatientFormViewModel>(vm => vm.Initialize(SelectedPatient)),
            () => SelectedPatient is not null);
        ViewProfileCommand = new RelayCommand(
            () => _navigationService.NavigateTo<PatientProfileViewModel>(vm => _ = vm.LoadAsync(SelectedPatient!.PatientId)),
            () => SelectedPatient is not null);
        DeactivateCommand = new RelayCommand(
            async () => await DeactivateSelectedAsync(),
            () => SelectedPatient is { IsActive: true });

        _ = LoadPatientsAsync();
    }

    public ObservableCollection<Patient> Patients { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool ShowInactive
    {
        get => _showInactive;
        set
        {
            if (SetProperty(ref _showInactive, value))
                _ = LoadPatientsAsync();
        }
    }

    public Patient? SelectedPatient
    {
        get => _selectedPatient;
        set => SetProperty(ref _selectedPatient, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand NewPatientCommand { get; }
    public ICommand EditPatientCommand { get; }
    public ICommand ViewProfileCommand { get; }
    public ICommand DeactivateCommand { get; }

    public async Task LoadPatientsAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _patientService.SearchAsync(SearchQuery, ShowInactive);
            Patients.Clear();
            foreach (var p in results)
                Patients.Add(p);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeactivateSelectedAsync()
    {
        if (SelectedPatient is null) return;
        await _patientService.DeactivateAsync(SelectedPatient.PatientId);
        await LoadPatientsAsync();
    }
}

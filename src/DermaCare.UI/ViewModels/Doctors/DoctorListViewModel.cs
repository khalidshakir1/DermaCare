using System.Collections.ObjectModel;
using System.Windows.Input;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using DermaCare.UI.Commands;
using DermaCare.UI.Helpers;

namespace DermaCare.UI.ViewModels.Doctors;

public class DoctorListViewModel : BaseViewModel
{
    private readonly IDoctorService _doctorService;
    private readonly INavigationService _navigationService;
    private Doctor? _selectedDoctor;

    public DoctorListViewModel(IDoctorService doctorService, INavigationService navigationService)
    {
        _doctorService = doctorService;
        _navigationService = navigationService;

        Doctors = new ObservableCollection<Doctor>();
        NewDoctorCommand = new RelayCommand(() =>
            _navigationService.NavigateTo<DoctorFormViewModel>(vm => _ = vm.InitializeAsync(null)));
        EditDoctorCommand = new RelayCommand(
            () => _navigationService.NavigateTo<DoctorFormViewModel>(vm => _ = vm.InitializeAsync(SelectedDoctor)),
            () => SelectedDoctor is not null);
        RefreshCommand = new RelayCommand(async () => await LoadAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<Doctor> Doctors { get; }

    public Doctor? SelectedDoctor
    {
        get => _selectedDoctor;
        set => SetProperty(ref _selectedDoctor, value);
    }

    public ICommand NewDoctorCommand { get; }
    public ICommand EditDoctorCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        var items = await _doctorService.GetAllActiveAsync();
        Doctors.Clear();
        foreach (var d in items)
            Doctors.Add(d);
    }
}

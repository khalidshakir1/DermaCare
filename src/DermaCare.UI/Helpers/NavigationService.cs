using DermaCare.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DermaCare.UI.Helpers;

public interface INavigationService
{
    BaseViewModel? CurrentView { get; }
    event Action? CurrentViewChanged;
    void NavigateTo<TViewModel>(Action<TViewModel>? initialize = null) where TViewModel : BaseViewModel;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private BaseViewModel? _currentView;

    public NavigationService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public BaseViewModel? CurrentView
    {
        get => _currentView;
        private set
        {
            _currentView = value;
            CurrentViewChanged?.Invoke();
        }
    }

    public event Action? CurrentViewChanged;

    public void NavigateTo<TViewModel>(Action<TViewModel>? initialize = null) where TViewModel : BaseViewModel
    {
        var vm = _serviceProvider.GetRequiredService<TViewModel>();
        initialize?.Invoke(vm);
        CurrentView = vm;
    }
}

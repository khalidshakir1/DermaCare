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
    private IServiceScope? _currentScope;
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
        var oldScope = _currentScope;
        var newScope = _serviceProvider.CreateScope();
        _currentScope = newScope;

        var vm = newScope.ServiceProvider.GetRequiredService<TViewModel>();
        initialize?.Invoke(vm);
        CurrentView = vm;

        oldScope?.Dispose();
    }
}

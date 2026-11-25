using System.Windows;
using DermaCare.UI.ViewModels;

namespace DermaCare.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

using System.Windows;
using HostelManagementSystem.ViewModels;

namespace HostelManagementSystem.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        DataContext = new SplashWindowViewModel();
    }
}

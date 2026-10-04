using HostelManagementSystem.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HostelManagementSystem.Views;

public partial class LicenseActivationView : UserControl
{
    private readonly LicenseActivationViewModel _viewModel;

    public event EventHandler? ActivationSucceeded;

    public LicenseActivationView()
    {
        InitializeComponent();

        _viewModel =
            new LicenseActivationViewModel();

        DataContext =
            _viewModel;

        _viewModel.ActivationSucceeded +=
            ViewModel_ActivationSucceeded;
    }

    private void ViewModel_ActivationSucceeded(
        object? sender,
        EventArgs e)
    {
        ActivationSucceeded?.Invoke(
            this,
            EventArgs.Empty
        );
    }

    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Window.GetWindow(this)?.DragMove();
        }
    }
}
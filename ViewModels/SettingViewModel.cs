using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.ViewModels.SettingViewModels;

namespace HostelManagementSystem.ViewModels;

public partial class SettingViewModel : ObservableObject
{
    public ThemeSettingsViewModel Theme { get; }

    public LicenseSettingsViewModel License { get; }

    [ObservableProperty]
    private ObservableObject _currentSettingViewModel;

    public SettingViewModel()
    {
        Theme = new ThemeSettingsViewModel();
        License = new LicenseSettingsViewModel();

        _currentSettingViewModel = Theme;
    }

    [RelayCommand]
    private void SelectTheme()
    {
        CurrentSettingViewModel = Theme;
    }

    [RelayCommand]
    private void SelectLicense()
    {
        CurrentSettingViewModel = License;
    }
}
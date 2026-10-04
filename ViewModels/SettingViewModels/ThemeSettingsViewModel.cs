using CommunityToolkit.Mvvm.ComponentModel;
using HostelManagementSystem.Helpers;

namespace HostelManagementSystem.ViewModels.SettingViewModels;

public partial class ThemeSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkMode;

    public ThemeSettingsViewModel()
    {
        IsDarkMode = ThemeManager.IsDarkMode;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        ThemeManager.SetTheme(value);
    }
}
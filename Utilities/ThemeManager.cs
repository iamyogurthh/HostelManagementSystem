using System;
using System.Linq;
using System.Windows;

namespace HostelManagementSystem.Helpers
{
    public static class ThemeManager
    {
        public static bool IsDarkMode => SettingsManager.Config.IsDarkMode;

        public static void SetTheme(bool isDark)
        {
            // 1. Update memory state & save to disk asynchronously
            SettingsManager.Config.IsDarkMode = isDark;
            SettingsManager.SaveAsync();

            // 2. Swap theme dictionaries in memory
            string themePath = isDark ? "Styles/Themes/DarkTheme.xaml" : "Styles/Themes/LightTheme.xaml";
            var newThemeDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

            var appResources = Application.Current.Resources.MergedDictionaries;
            var existingTheme = appResources.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("LightTheme.xaml") ||
                                     d.Source.OriginalString.Contains("DarkTheme.xaml")));

            if (existingTheme != null)
                appResources.Remove(existingTheme);

            appResources.Add(newThemeDict);
        }

        public static void ApplySavedTheme()
        {
            SettingsManager.Load();
            SetTheme(IsDarkMode);
        }
    }
}
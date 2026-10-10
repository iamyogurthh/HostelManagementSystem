using CommunityToolkit.Mvvm.ComponentModel;
using HostelManagementSystem.Models;
using System.Collections.ObjectModel;

namespace HostelManagementSystem.ViewModels.SettingViewModels
{
    public partial class VersionSettingsViewModel : ObservableObject
    {
        public string CurrentVersion => AppVersionInfo.CurrentVersion;

        public ObservableCollection<AppRelease> Releases { get; } =
        [
            new AppRelease
            {
                Version = AppVersionInfo.CurrentVersion,
                ReleaseDate = "October 9, 2026",
                ReleaseType = "Latest Release",
                ReleaseNotes =
                [
                    "Added payment management.",
                    "Improved room assignment.",
                    "Fixed resident list filtering."
                ]
            },
            new AppRelease
            {
                Version = "dev1.1.0",
                ReleaseDate = "September 20, 2026",
                ReleaseType = "Previous Release",
                ReleaseNotes =
                [
                    "Added student category management.",
                    "Improved dashboard performance."
                ]
            },
            new AppRelease
            {
                Version = "dev1.0.0",
                ReleaseDate = "September 1, 2026",
                ReleaseType = "Initial Release",
                ReleaseNotes =
                [
                    "Introduced resident management.",
                    "Added building, floor, and room management."
                ]
            }
        ];
    }

    public class AppRelease
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string ReleaseType { get; set; } = string.Empty;
        public ObservableCollection<string> ReleaseNotes { get; set; } = [];
    }
}
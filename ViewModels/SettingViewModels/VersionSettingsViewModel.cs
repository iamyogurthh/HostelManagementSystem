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
                ReleaseDate = "November 12, 2026",
                ReleaseType = "Latest Release",
                ReleaseNotes =
                [
                    "Added GitHub Release update notifications for newer stable versions.",
                    "Added automated Windows installer creation with Inno Setup.",
                    "Added GitHub Actions release automation for self-contained win-x64 builds.",
                    "Added Save Image buttons for student profile, NRC, and certificate images.",
                    "Improved student image save filenames by including the student's name.",
                    "Fixed student profile image Save Image button placement.",
                    "Fixed room default image display when the stored image path is empty or invalid.",
                    "Improved automatic search and filter behavior with debounce support.",
                    "Fixed Settings navigation so the License tab and content reset correctly.",
                    "Improved application version display by removing the Git commit hash.",
                    "Improved room assignment and student detail usability.",
                    "Fixed resident list filtering."
                ]
            },
            new AppRelease
            { 
                Version = "1.2.1",
                ReleaseDate = "November 5, 2026",
                ReleaseType = "Feature Release",
                ReleaseNotes =
                [
                    "Added manual billing and payment management.",
                    "Added fee-rule support for manual charges.",
                    "Improved student search performance with server-side filtering.",
                    "Added automatic search and filter updates with debounce support.",
                    "Improved building, floor, and room management.",
                    "Prevented deletion of buildings, floors, and rooms containing students.",
                    "Fixed data import and export compatibility after the billing migration.",
                    "Added startup splash screen with a visual loading indicator.",
                    "Added persistent light and dark theme support.",
                    "Improved Settings navigation and default License tab selection.",
                    "Fixed default images for rooms when image paths are missing or invalid.",
                    "Improved the room assignment experience.",
                    "Fixed resident list filtering."
                ]
            },
            new AppRelease
            {
                Version = "1.2.0",
                ReleaseDate = "October 9, 2026",
                ReleaseType = "Feature Release",
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
                ReleaseType = "Feature Release",
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
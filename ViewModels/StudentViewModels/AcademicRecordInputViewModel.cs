using CommunityToolkit.Mvvm.ComponentModel;

namespace HostelManagementSystem.ViewModels.StudentViewModels
{
    public partial class AcademicRecordInputViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _academicYearLevel = "ပထမနှစ်";

        [ObservableProperty]
        private int _attemptNumber = 1;

        [ObservableProperty]
        private string _rollNumber = string.Empty;

        [ObservableProperty]
        private string _attendedYear = string.Empty;

        [ObservableProperty]
        private bool _isHonours;
    }
}
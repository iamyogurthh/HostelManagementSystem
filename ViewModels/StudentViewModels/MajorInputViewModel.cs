using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HostelManagementSystem.ViewModels.StudentViewModels
{
    public partial class MajorInputViewModel : ObservableObject
    {
        public ObservableCollection<string> Majors { get; } = new();

        [ObservableProperty]
        private string _selectedMajor = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public MajorInputViewModel()
        {
            InitializeDataSources();
        }

        private void InitializeDataSources()
        {
            Majors.Clear();

            // Yadanarbon University (ရတနာပုံ တက္ကသိုလ်) Official Majors List
            var yadanarbonMajors = new[]
             {
                // Arts Specialties
                "Myanmar (မြန်မာစာ)",
                "English (အင်္ဂလိပ်စာ)",
                "Geography (ပထဝီဝင်)",
                "History (သမိုင်း)",
                "Philosophy (တက္ကသိုလ် ဒဿနိကဗေဒ)",
                "Psychology (စိတ်ပညာ)",
                "Archaeology (ရှေးဟောင်းသုတေသန)",
                "Anthropology (လူသားဗေဒ)",
                "Law (ဥပဒေပညာ)",
                "Oriental Studies (ပါဠိ/မဂဓ)",
                "International Relations (နိုင်ငံတကာဆက်ဆံရေး)",
                "Economics (ဘောဂဗေဒ)",

                // Science Specialties
                "Chemistry (ဓာတုဗေဒ)",
                "Biochemistry (ဇီဝဓာတုဗေဒ)",
                "Physics (ရူပဗေဒ)",
                "Nuclear Physics (နျူကလီးယားရူပဗေဒ)",
                "Mathematics (သင်္ချာ)",
                "Zoology (သတ္တဗေဒ)",
                "Botany (ရက္ခဗေဒ)",
                "Microbiology (အဏုဇီဝဗေဒ)",
                "Geology (ဘူမိဗေဒ)",
                "Industrial Chemistry (စက်မှုဓာတုဗေဒ)",
                "Computer Science (ကွန်ပျူတာသိပ္ပံ)",
                "Environmental Studies (ပတ်ဝန်းကျင်ထိန်းသိမ်းရေးပညာ)",
                "Biotechnology (ဇီဝနည်းပညာ)",

                // Other Specialties
                "Other (အခြား)"
            };

            foreach (var major in yadanarbonMajors)
            {
                Majors.Add(major);
            }

            SelectedMajor = Majors.FirstOrDefault() ?? string.Empty;
        }

        public bool Validate(string label = "Major")
        {
            if (string.IsNullOrWhiteSpace(SelectedMajor))
            {
                ErrorMessage = $"ကျေးဇူးပြု၍ {label} ရွေးချယ်ပါ။";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }
    }
}
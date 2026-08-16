using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Messages;

namespace HostelManagementSystem.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<NavigateMessage>
    {
        // Tracks whichever page is currently displayed in the ContentFrame
        [ObservableProperty]
        private ObservableObject _currentView;

        public DashboardViewModel Dashboard { get; }
        public BuildingViewModel Building { get; }
        public ResidentViewModel Resident { get; }
        public SettingViewModel Setting { get; }

        

        // Constructor 
        public MainViewModel()
        {
            Dashboard = new DashboardViewModel();
            Building = new BuildingViewModel();
            Resident = new ResidentViewModel();
            Setting = new SettingViewModel();

            WeakReferenceMessenger.Default.Register(this);

            // Set default initial view 
            CurrentView = Dashboard;
        }

        // CommunityToolkit generates "NavigateToDashboardCommand"
        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentView = Dashboard;
        }

        // CommunityToolkit generates "NavigateToBuildingCommand"
        [RelayCommand]
        private void NavigateToBuilding()
        {
            CurrentView = Building;
        }

        [RelayCommand]
        private void NavigateToResident() 
        {
            CurrentView = Resident;
        }

        [RelayCommand]
        private void NavigateToSetting()
        {
            CurrentView = Setting;
        }

        public void Receive(NavigateMessage message)
        {
            CurrentView = message.TargetViewModel;
        }
    }
}
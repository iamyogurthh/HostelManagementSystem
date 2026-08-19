using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Messages;

namespace HostelManagementSystem.ViewModels
{
    public partial class MainViewModel : ObservableObject,
        IRecipient<NavigateMessage>,
        IRecipient<BusyMessage>
    {
        // Tracks whichever page is currently displayed in the ContentFrame
        [ObservableProperty]
        private ObservableObject _currentView;

        public DashboardViewModel Dashboard { get; }
        public BuildingViewModel Building { get; }
        public ResidentViewModel Resident { get; }
        public SettingViewModel Setting { get; }

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyText = "ခဏစောင့်ပါ...";

        // Constructor 
        public MainViewModel()
        {
            Dashboard = new DashboardViewModel();
            Building = new BuildingViewModel();
            Resident = new ResidentViewModel();
            Setting = new SettingViewModel();

            // Registering 'this' subscribes to ALL IRecipient<T> implementations automatically
            WeakReferenceMessenger.Default.Register<NavigateMessage>(this);
            WeakReferenceMessenger.Default.Register<BusyMessage>(this);

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
        private async Task NavigateToBuilding()
        {
            CurrentView = Building;
            await Building.LoadBuildingCommand.ExecuteAsync(null);
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
            IsBusy = false;
            CurrentView = message.TargetViewModel;
        }

        public void Receive(BusyMessage message)
        {
            IsBusy = message.IsBusy;
            BusyText = message.Message;
        }
    }
}
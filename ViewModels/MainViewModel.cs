using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Messages;

namespace HostelManagementSystem.ViewModels
{
    public partial class MainViewModel : ObservableRecipient,
        IRecipient<NavigateMessage>,
        IRecipient<BusyMessage>
    {
        [ObservableProperty]
        private ObservableObject _currentView = null!;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyText = "ခဏစောင့်ပါ...";

        public DashboardViewModel Dashboard { get; }
        public BuildingViewModel Building { get; }
        public ResidentViewModel Resident { get; }
        public SettingViewModel Setting { get; }
        public DataTransferViewModel DataTransfer { get; } 

        public MainViewModel(
            DashboardViewModel dashboard,
            BuildingViewModel building,
            ResidentViewModel resident,
            SettingViewModel setting,
            DataTransferViewModel dataTransfer)
        {
            Dashboard = dashboard;
            Building = building;
            Resident = resident;
            Setting = setting;
            DataTransfer = dataTransfer;

            CurrentView = Dashboard;
            IsActive = true;


        }

        public MainViewModel() : this(
            new DashboardViewModel(),
            new BuildingViewModel(),
            new ResidentViewModel(),
            new SettingViewModel(),
            new DataTransferViewModel())
        {
        }

        /// <summary>
        /// Single unified navigation command. Accepts view names like "Dashboard", "Building", "Resident", "Setting".
        /// </summary>
        [RelayCommand]
        private async Task NavigateToAsync(string? viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName)) return;

            switch (viewName.ToLowerInvariant())
            {
                case "dashboard":
                    SetCurrentView(Dashboard);
                    break;

                case "building":
                    SetCurrentView(Building);
                    if (Building.LoadBuildingCommand.CanExecute(null))
                    {
                        await Building.LoadBuildingCommand.ExecuteAsync(null);
                    }
                    break;

                case "resident":
                    SetCurrentView(Resident);

                    if (Resident.LoadDataCommand.CanExecute(null))
                    {
                        await Resident.LoadDataCommand.ExecuteAsync(null);
                    }

                    break;

                case "setting":
                    SetCurrentView(Setting);
                    break;

                case "datatransfer":
                    SetCurrentView(DataTransfer);
                    break;
            }
        }

        private void SetCurrentView(ObservableObject targetViewModel)
        {
            if (CurrentView != targetViewModel)
            {
                CurrentView = targetViewModel;
            }
        }

        public void Receive(NavigateMessage message)
        {
            IsBusy = false;
            if (message.TargetViewModel != null)
            {
                SetCurrentView(message.TargetViewModel);
            }
        }

        public void Receive(BusyMessage message)
        {
            IsBusy = message.IsBusy;
            if (!string.IsNullOrWhiteSpace(message.Message))
            {
                BusyText = message.Message;
            }
        }
    }
}
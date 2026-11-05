using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Helpers;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Services;
using HostelManagementSystem.ViewModels.FeeRuleViewModels;
using HostelManagementSystem.ViewModels.PaymentViewModels;
using HostelManagementSystem.Models;

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

        [ObservableProperty]
        private bool _isDarkMode = ThemeManager.IsDarkMode;

        public DashboardViewModel Dashboard { get; }
        public BuildingViewModel Building { get; }
        public ResidentViewModel Resident { get; }
        public SettingViewModel Setting { get; }
        public DataTransferViewModel DataTransfer { get; } 
        public FeeRuleViewModel FeeRule { get; }
        public PaymentViewModel Payment { get; }

        public string CurrentVersion => AppVersionInfo.CurrentVersion;

        public string LicenseRemainingText =>
            GetLicenseRemainingText();

        partial void OnIsDarkModeChanged(bool value)
        {
            ThemeManager.SetTheme(value);
        }

        public MainViewModel(
            DashboardViewModel dashboard,
            BuildingViewModel building,
            ResidentViewModel resident,
            SettingViewModel setting,
            DataTransferViewModel dataTransfer,
            FeeRuleViewModel feeRule,
            PaymentViewModel payment)
        {
            Dashboard = dashboard;
            Building = building;
            Resident = resident;
            Setting = setting;
            DataTransfer = dataTransfer;
            FeeRule = feeRule;
            Payment = payment;

            CurrentView = Dashboard;
            IsActive = true;


        }

        private static string GetLicenseRemainingText()
        {
            var license = new LicenseStorage().Load();

            if (license?.ExpiresAt == null)
            {
                return "သက်တမ်းကုန်ဆုံးရက်မရှိ";
            }

            var remaining = license.ExpiresAt.Value.ToUniversalTime() - DateTime.UtcNow;
            var remainingDays = Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));

            return $"{remainingDays}ရက်ကျန်";
        }

        public MainViewModel() : this(
            new DashboardViewModel(),
            new BuildingViewModel(),
            new ResidentViewModel(),
            new SettingViewModel(),
            new DataTransferViewModel(),
            new FeeRuleViewModel(),
            new PaymentViewModel())
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
                    Setting.ResetToDefaultView();
                    SetCurrentView(Setting);
                    break;

                case "datatransfer":
                    SetCurrentView(DataTransfer);
                    break;

                case "feerule":
                    SetCurrentView(FeeRule);
                    if (FeeRule.LoadFeeRulesCommand.CanExecute(null))
                    {
                        await FeeRule.LoadFeeRulesCommand.ExecuteAsync(null);
                    }       
                    break;

                case "payment": SetCurrentView(Payment); 
                    if (Payment.LoadStudentsCommand.CanExecute(null)) 
                    { 
                        await Payment.LoadStudentsCommand.ExecuteAsync(null); 
                    } 
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
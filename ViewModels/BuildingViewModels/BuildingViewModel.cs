using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels
{
    public partial class BuildingViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasBuildings))]
        [NotifyPropertyChangedFor(nameof(HasNoBuildings))]
        private ObservableCollection<Building> _buildings = new();

        public bool HasBuildings => Buildings != null && Buildings.Count > 0;
        public bool HasNoBuildings => !HasBuildings;

        public BuildingViewModel()
        {
            // Subscribe once to handle additions/deletions at runtime
            Buildings.CollectionChanged += (s, e) => NotifyStateChanges();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasBuildings));
            OnPropertyChanged(nameof(HasNoBuildings));
        }

        [RelayCommand]
        public async Task LoadBuildingAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                using var db = new AppDbContext();

                var buildingList = await db.Buildings
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .ToListAsync();

                // Clear and repopulate to retain existing ObservableCollection reference
                Buildings.Clear();
                foreach (var b in buildingList)
                {
                    Buildings.Add(b);
                }

                NotifyStateChanges();
            }, "အဆောင်အချက်အလက်များ ရယူနေပါသည်...");
        }

        [RelayCommand]
        public async Task DeleteBuildingAsync(Building building)
        {
            if (building == null) return;

            // TODO: Replace with resident check logic upon Resident model implementation
            bool hasOccupiedRooms = false;

            if (hasOccupiedRooms)
            {
                MessageBox.Show(
                    "ဤအဆောင်တွင် လူနေထိုင်လျက်ရှိသော အခန်းများ ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                return;
            }

            // 1. Confirm dialog BEFORE triggering loading overlay
            var result = MessageBox.Show(
                $"\"{building.Name}\" အဆောင်ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အဆောင်ဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // 2. Perform DB deletion inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();

                    var targetBuilding = await db.Buildings
                        .Include(b => b.Floors)
                        .ThenInclude(f => f.Rooms)
                        .FirstOrDefaultAsync(b => b.Id == building.Id);

                    if (targetBuilding != null)
                    {
                        // Cascade soft delete
                        targetBuilding.IsActive = false;

                        foreach (var floor in targetBuilding.Floors)
                        {
                            floor.IsActive = false;

                            foreach (var room in floor.Rooms)
                            {
                                room.IsActive = false;
                            }
                        }

                        await db.SaveChangesAsync();

                        // UI automatically updates HasBuildings/HasNoBuildings via CollectionChanged
                        Buildings.Remove(building);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"ဖျက်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                        "အမှား",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }, "အဆောင်ဖျက်နေပါသည်...");
        }

        [RelayCommand]
        private async Task SelectBuilding(Building selectedBuilding)
        {
            if (selectedBuilding == null) return;

            var floorViewModel = new FloorViewModel(selectedBuilding);

            // 1. Switch View
            WeakReferenceMessenger.Default.Send(new NavigateMessage(floorViewModel));

            // 2. Load Floors with Spinner
            await floorViewModel.LoadFloorsByBuildingIdCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void OpenCreateBuildingForm()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new CreateBuildingFormViewModel()));
        }

        [RelayCommand]
        private void OpenEditBuildingForm(Building building)
        {
            if (building == null) return;
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new EditBuildingFormViewModel(building)));
        }
    }
}
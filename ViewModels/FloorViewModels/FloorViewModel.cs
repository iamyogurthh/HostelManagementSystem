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
    public partial class FloorViewModel : ObservableObject
    {
        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFloors))]
        [NotifyPropertyChangedFor(nameof(HasNoFloors))]
        private ObservableCollection<Floor> _floors = new();

        public bool HasFloors => Floors != null && Floors.Count > 0;
        public bool HasNoFloors => !HasFloors;

        public FloorViewModel(Building selectedBuilding)
        {
            Building = selectedBuilding ?? throw new ArgumentNullException(nameof(selectedBuilding));

            // Subscribe once to handle additions/deletions at runtime
            Floors.CollectionChanged += (s, e) => NotifyStateChanges();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasFloors));
            OnPropertyChanged(nameof(HasNoFloors));
        }

        [RelayCommand]
        public async Task LoadFloorsByBuildingIdAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                using var db = new AppDbContext();

                var floorList = await db.Floors
                    .AsNoTracking()
                    .Where(f => f.IsActive && f.BuildingId == Building.Id)
                    .ToListAsync();

                // Clear and repopulate to retain the existing ObservableCollection reference
                Floors.Clear();
                foreach (var floor in floorList)
                {
                    Floors.Add(floor);
                }

                NotifyStateChanges();
            }, "အထပ်အချက်အလက်များ ရယူနေပါသည်...");
        }

        [RelayCommand]
        public async Task DeleteFloorAsync(Floor floor)
        {
            if (floor == null) return;

            await using var occupancyDb = new AppDbContext();
            var hasOccupiedRooms = await occupancyDb.StudentAccommodations
                .AnyAsync(a =>
                    a.IsActive &&
                    a.EndDate == null &&
                    a.Room.FloorId == floor.Id);

            if (hasOccupiedRooms)
            {
                MessageBox.Show(
                    "ဤအထပ်တွင် လူနေထိုင်လျက်ရှိသော အခန်းများ ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                return;
            }

            // 1. Ask confirmation FIRST (before spinner starts)
            var result = MessageBox.Show(
                $"\"{floor.Name}\" ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အထပ်ဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // 2. Wrap soft-delete operation in LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();

                    var targetFloor = await db.Floors
                        .Include(f => f.Rooms)
                        .FirstOrDefaultAsync(f => f.Id == floor.Id);

                    if (targetFloor != null)
                    {
                        var stillOccupied = await db.StudentAccommodations
                            .AnyAsync(a =>
                                a.IsActive &&
                                a.EndDate == null &&
                                a.Room.FloorId == floor.Id);

                        if (stillOccupied)
                        {
                            MessageBox.Show(
                                "ဤအထပ်တွင် လူနေထိုင်လျက်ရှိသော အခန်းများ ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။",
                                "သတိပေးချက်",
                                MessageBoxButton.OK,
                                MessageBoxImage.Stop);
                            return;
                        }

                        targetFloor.IsActive = false;

                        // Soft delete all child rooms under this floor
                        foreach (var room in targetFloor.Rooms)
                        {
                            room.IsActive = false;
                        }

                        await db.SaveChangesAsync();

                        // Automatically triggers HasFloors/HasNoFloors state update via CollectionChanged
                        Floors.Remove(floor);
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
            }, "အထပ်ဖျက်နေပါသည်...");
        }

        [RelayCommand]
        private async Task Back() // Navigate back to Building Page and load items
        {
            var buildingViewModel = new BuildingViewModel();
            WeakReferenceMessenger.Default.Send(new NavigateMessage(buildingViewModel));
            await buildingViewModel.LoadBuildingCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void OpenCreateFloorForm()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new CreateFloorFormViewModel(Building)));
        }

        [RelayCommand]
        private void OpenEditFloorForm(Floor floor)
        {
            if (floor == null) return;
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new EditFloorFormViewModel(floor)));
        }

        [RelayCommand]
        private async Task SelectFloor(Floor selectedFloor)
        {
            if (selectedFloor == null) return;

            var roomViewModel = new RoomViewModel(Building, selectedFloor);

            //Switch View to RoomView
            WeakReferenceMessenger.Default.Send(new NavigateMessage(roomViewModel));

            //Load Rooms 
            await roomViewModel.LoadRoomsByFloorIdCommand.ExecuteAsync(null);
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

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

            // Automatically load floors for the passed building upon initialization
            LoadFloorsByBuildingId(Building.Id);
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasFloors));
            OnPropertyChanged(nameof(HasNoFloors));
        }

        [RelayCommand]
        public void LoadFloorsByBuildingId(int buildingId)
        {
            using var db = new AppDbContext();

            var floorList = db.Floors
                .Include(f => f.Building)
                .Where(f => f.IsActive && f.BuildingId == buildingId)
                .ToList();

            // Clear and repopulate to retain the existing ObservableCollection reference
            Floors.Clear();
            foreach (var floor in floorList)
            {
                Floors.Add(floor);
            }

            NotifyStateChanges();
        }

        [RelayCommand]
        public void DeleteFloor(Floor floor)
        {
            if (floor == null) return;

            using var db = new AppDbContext();

            // TODO: Replace with resident check logic upon Resident model implementation
            bool hasOccupiedRooms = false;

            if (hasOccupiedRooms)
            {
                MessageBox.Show(
                    "ဤအထပ်တွင် လူနေထိုင်လျက်ရှိသော အခန်းများ ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                return;
            }

            var result = MessageBox.Show(
                $"\"{floor.Name}\" ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အထပ်ဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var targetFloor = db.Floors
                    .Include(f => f.Rooms)
                    .FirstOrDefault(f => f.Id == floor.Id);

                if (targetFloor != null)
                {
                    targetFloor.IsActive = false;

                    // Soft delete all child rooms under this floor
                    foreach (var room in targetFloor.Rooms)
                    {
                        room.IsActive = false;
                    }

                    db.SaveChanges();

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
        }

        [RelayCommand]
        private void Back() // Navigate back to Building Page
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new BuildingViewModel()));
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
    }
}
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
            LoadBuilding();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasBuildings));
            OnPropertyChanged(nameof(HasNoBuildings));
        }

        [RelayCommand]
        public void LoadBuilding()
        {
            using var db = new AppDbContext();

            var buildingList = db.Buildings
                .Where(b => b.IsActive)
                .ToList();

            // Clear and repopulate to retain the existing ObservableCollection reference
            Buildings.Clear();
            foreach (var b in buildingList)
            {
                Buildings.Add(b);
            }

            NotifyStateChanges();
        }

        [RelayCommand]
        public void DeleteBuilding(Building building)
        {
            if (building == null) return;

            using var db = new AppDbContext();

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

            var result = MessageBox.Show(
                $"\"{building.Name}\" အဆောင်ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အဆောင်ဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var targetBuilding = db.Buildings
                    .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms)
                    .FirstOrDefault(b => b.Id == building.Id);

                if (targetBuilding != null)
                {
                    targetBuilding.IsActive = false;

                    foreach (var floor in targetBuilding.Floors)
                    {
                        floor.IsActive = false;

                        foreach (var room in floor.Rooms)
                        {
                            room.IsActive = false;
                        }
                    }

                    db.SaveChanges();

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
        }

        [RelayCommand]
        private void SelectBuilding(Building selectedBuilding)
        {
            if (selectedBuilding == null)
            {
                return;
            } else
            {
                WeakReferenceMessenger.Default.Send(new NavigateMessage(new FloorViewModel(selectedBuilding)));
            }

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
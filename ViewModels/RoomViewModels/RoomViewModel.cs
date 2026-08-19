using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.RoomDetailViewModels;
using HostelManagementSystem.ViewModels.RoomViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels
{
    public partial class RoomViewModel : ObservableObject
    {
        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRooms))]
        [NotifyPropertyChangedFor(nameof(HasNoRooms))]
        private ObservableCollection<Room> _rooms = new();

        public bool HasRooms => Rooms != null && Rooms.Count > 0;
        public bool HasNoRooms => !HasRooms;

        public RoomViewModel(Building building, Floor floor)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));

            // Subscribe once to handle additions/deletions at runtime
            Rooms.CollectionChanged += (s, e) => NotifyStateChanges();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasRooms));
            OnPropertyChanged(nameof(HasNoRooms));
        }

        [RelayCommand]
        public async Task LoadRoomsByFloorIdAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                using var db = new AppDbContext();

                var roomList = await db.Rooms
                    .AsNoTracking()
                    .Where(r => r.IsActive && r.FloorId == Floor.Id)
                    .ToListAsync();

                // Clear and repopulate to retain existing ObservableCollection reference
                Rooms.Clear();
                foreach (var room in roomList)
                {
                    Rooms.Add(room);
                }

                NotifyStateChanges();
            }, "အခန်းအချက်အလက်များ ရယူနေပါသည်...");
        }

        [RelayCommand]
        public async Task DeleteRoomAsync(Room room)
        {
            if (room == null) return;

            // TODO: Replace with resident check logic upon Resident model implementation
            bool hasOccupants = false;

            if (hasOccupants)
            {
                MessageBox.Show(
                    "ဤအခန်းတွင် လူနေထိုင်လျက်ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                return;
            }

            // 1. Ask confirmation FIRST (before spinner starts)
            var result = MessageBox.Show(
                $"\"{room.Name}\" ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အခန်းဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // 2. Wrap soft-delete in LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();
                    var targetRoom = await db.Rooms.FirstOrDefaultAsync(r => r.Id == room.Id);

                    if (targetRoom != null)
                    {
                        // Soft delete
                        targetRoom.IsActive = false;
                        await db.SaveChangesAsync();

                        // Automatically triggers HasRooms/HasNoRooms state update via CollectionChanged
                        Rooms.Remove(room);
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
            }, "အခန်းဖျက်နေပါသည်...");
        }

        [RelayCommand]
        private async Task Back() // Navigate back to Floor Page and refresh list
        {
            var floorViewModel = new FloorViewModel(Building);

            // 1. Switch View
            WeakReferenceMessenger.Default.Send(new NavigateMessage(floorViewModel));

            // 2. Load Floors
            await floorViewModel.LoadFloorsByBuildingIdCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void OpenCreateRoomForm()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new CreateRoomFormViewModel(Building, Floor)));
        }

        [RelayCommand]
        private void OpenEditRoomForm(Room room)
        {
            if (room == null) return;
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new EditRoomFormViewModel(Building, Floor, room)));
        }

        [RelayCommand]
        private async Task SelectRoom(Room selectedRoom)
        {
            if(selectedRoom == null) return;

            var roomDetailViewModel = new RoomDetailViewModel(Building, Floor, selectedRoom);

            //Switch View to RoomDetailView
            WeakReferenceMessenger.Default.Send(new NavigateMessage(roomDetailViewModel));

            //Load Room detail
        }
    }
}
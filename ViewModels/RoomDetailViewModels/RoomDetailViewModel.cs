using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace HostelManagementSystem.ViewModels.RoomDetailViewModels
{
    public partial class RoomDetailViewModel: ObservableObject
    {
        [ObservableProperty]
        private Room _room;

        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        public RoomDetailViewModel(Building building, Floor floor, Room room) { 
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));
            Room = room ?? throw new ArgumentNullException(nameof(room));
        }

        [RelayCommand]
        public async Task LoadRoomDetailByRoomIdAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                using var db = new AppDbContext();

                //implement  getting resident list living in a room

            }, "အခန်းအချက်အလက်များ ရယူနေပါသည်...");
        }

        [RelayCommand]
        private async Task Back() // Navigate back to Room Page and refresh list
        {
            var roomViewModel = new RoomViewModel(Building, Floor);

            // 1. Switch View
            WeakReferenceMessenger.Default.Send(new NavigateMessage(roomViewModel));

            // 2. Load Floors
            await roomViewModel.LoadRoomsByFloorIdCommand.ExecuteAsync(null);
        }

    }
}

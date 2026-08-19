using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels.RoomViewModels
{
    public partial class CreateRoomFormViewModel : RoomFormBaseViewModel
    {
        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        public CreateRoomFormViewModel(Building building, Floor floor)
        {
            Building = building;
            Floor = floor;
        }

        [RelayCommand]
        public override async Task SaveAsync()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            RoomNameError = GetErrors(nameof(RoomName))
                .FirstOrDefault()?.ErrorMessage;

            if (string.IsNullOrEmpty(GetErrors(nameof(RoomCapacityText)).FirstOrDefault()?.ErrorMessage) && RoomCapacity > 1000)
            {
                RoomCapacityError = "လူဝင်ဆံ့မှုသည် ၁ မှ ၁၀၀၀ အတွင်း သာ ဖြစ်ရပါမည်";
            }
            else
            {
                RoomCapacityError = GetErrors(nameof(RoomCapacityText))
                    .FirstOrDefault()?.ErrorMessage;
            }

            if (HasErrors || !string.IsNullOrEmpty(RoomCapacityError))
            {
                return;
            }

            bool isSavedSuccessfully = false;

            // 1. Perform image saving and database insertion inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    string savedImagePath = SaveImageToAppData(ImgPath);
                    using var db = new AppDbContext();

                    var newRoom = new Room
                    {
                        Name = RoomName.Trim(),
                        Capacity = RoomCapacity,
                        Img = string.IsNullOrEmpty(savedImagePath) ? "pack://application:,,,/Image/DefaultImages/default_room.png" : savedImagePath,
                        IsActive = IsActive,
                        FloorId = Floor.Id,
                    };

                    await db.Rooms.AddAsync(newRoom);
                    await db.SaveChangesAsync();

                    isSavedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အခန်းအချက်အလက် သိမ်းဆည်းနေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has completed and hidden the spinner
            if (isSavedSuccessfully)
            {
                await Back();
            }
        }

        [RelayCommand]
        public async Task Back()
        {
            var roomViewModel = new RoomViewModel(Building, Floor);

            // 1. Switch View
            WeakReferenceMessenger.Default.Send(new NavigateMessage(roomViewModel));

            // 2. Reload Room list with spinner
            await roomViewModel.LoadRoomsByFloorIdCommand.ExecuteAsync(null);
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels.RoomViewModels
{
    public partial class EditRoomFormViewModel : RoomFormBaseViewModel
    {
        private readonly ImageManager _imageManager = new ImageManager();
        private const string RoomImagesFolder = "RoomImages";

        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        [ObservableProperty]
        private Room _room;

        public EditRoomFormViewModel(Building building, Floor floor, Room room)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));
            Room = room ?? throw new ArgumentNullException(nameof(room));

            // Pre-populate fields from existing entity
            RoomName = Room.Name;
            RoomCapacityText = Room.Capacity.ToString();
            ImgPath = Room.Img;
            PreviewImageSource = string.IsNullOrEmpty(Room.Img) ? DefaultRoomImg : Room.Img;
            IsActive = Room.IsActive;
        }

        [RelayCommand]
        public override async Task SaveAsync()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            RoomNameError = GetErrors(nameof(RoomName))
                .FirstOrDefault()?.ErrorMessage;

            // Handle capacity range validation manually if no initial regex/required errors exist
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

            bool isUpdatedSuccessfully = false;

            // 1. Perform image saving and database update inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();

                    var dbRoom = await db.Rooms.FirstOrDefaultAsync(r => r.Id == Room.Id);
                    if (dbRoom != null)
                    {
                        string oldImagePath = dbRoom.Img;

                        // Save image using ImageManager
                        string savedImagePath = _imageManager.SaveImage(
                            ImgPath,
                            oldImagePath,
                            RoomImagesFolder,
                            DefaultRoomImg
                        );

                        dbRoom.Name = RoomName.Trim();
                        dbRoom.Capacity = RoomCapacity;
                        dbRoom.Img = savedImagePath;
                        dbRoom.IsActive = IsActive;

                        await db.SaveChangesAsync();

                        // Clean up old image if a new image path was saved
                        if (savedImagePath != oldImagePath)
                        {
                            _imageManager.DeleteImage(oldImagePath, DefaultRoomImg);
                        }

                        isUpdatedSuccessfully = true;
                    }
                    else
                    {
                        ErrorMessage = "ပြင်ဆင်လိုသော အခန်းကို ရှာမတွေ့ပါ။";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"ပြင်ဆင်မှု သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အခန်းအချက်အလက် ပြင်ဆင်နေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has completed and hidden the spinner
            if (isUpdatedSuccessfully)
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
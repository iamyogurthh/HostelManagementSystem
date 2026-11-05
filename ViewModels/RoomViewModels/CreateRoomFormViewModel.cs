using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels.RoomViewModels
{
    public partial class CreateRoomFormViewModel : RoomFormBaseViewModel
    {
        private readonly ImageManager _imageManager = new ImageManager();
        private const string RoomImagesFolder = "RoomImages";

        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        [ObservableProperty]
        private ObservableCollection<RoomType> _roomTypes = new();

        [ObservableProperty]
        private RoomType? _selectedRoomType;

        [ObservableProperty]
        private string? _roomTypeError;

        public CreateRoomFormViewModel(Building building, Floor floor)
        {
            Building = building;
            Floor = floor;

            _ = LoadRoomTypesAsync();
        }

        private async Task LoadRoomTypesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var list = await db.RoomTypes.AsNoTracking().Where(r => r.IsActive == true).ToListAsync();
                RoomTypes = new ObservableCollection<RoomType>(list);

                if (RoomTypes.Any())
                {
                    SelectedRoomType = RoomTypes.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"အခန်းအမျိုးအစားများ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
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

            // Room Type Validation
            if (SelectedRoomType == null)
            {
                RoomTypeError = "အခန်းအမျိုးအစား ရွေးချယ်ပါ";
            }
            else
            {
                RoomTypeError = null;
            }

            if (HasErrors || !string.IsNullOrEmpty(RoomCapacityError) || !string.IsNullOrEmpty(RoomTypeError))
            {
                return;
            }

            bool isSavedSuccessfully = false;

            // 1. Perform image saving and database insertion inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    // Save image using ImageManager
                    string savedImagePath = _imageManager.SaveImage(
                        ImgPath,
                        existingDbPath: string.Empty,
                        subDirectory: RoomImagesFolder,
                        defaultImageName: DefaultRoomImg
                    );

                    using var db = new AppDbContext();

                    var newRoom = new Room
                    {
                        Name = RoomName.Trim(),
                        Capacity = RoomCapacity,
                        Img = savedImagePath,
                        IsActive = IsActive,
                        FloorId = Floor.Id,
                        RoomTypeId = SelectedRoomType!.Id
                    };

                    await db.Rooms.AddAsync(newRoom);
                    await db.SaveChangesAsync();

                    WeakReferenceMessenger.Default.Send(new DatabaseUpdatedMessage("Room", newRoom.Id, DatabaseOperation.Inserted));
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
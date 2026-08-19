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

namespace HostelManagementSystem.ViewModels
{
    public partial class EditFloorFormViewModel : FloorFormBaseViewModel
    {
        private readonly int _floorId;

        [ObservableProperty]
        private Building _building;

        public EditFloorFormViewModel(Floor floor)
        {
            if (floor == null) throw new ArgumentNullException(nameof(floor));

            _floorId = floor.Id;
            FloorName = floor.Name;
            IsActive = floor.IsActive;

            // Fetch parent building if navigation property is null
            if (floor.Building != null)
            {
                Building = floor.Building;
            }
            else
            {
                using var db = new AppDbContext();
                Building = db.Buildings.FirstOrDefault(b => b.Id == floor.BuildingId)
                           ?? throw new InvalidOperationException("Parent building not found.");
            }

            ImgPath = floor.Img ?? string.Empty;
            PreviewImageSource = !string.IsNullOrWhiteSpace(floor.Img)
                ? floor.Img
                : DefaultFloorImg;
        }

        [RelayCommand]
        public override async Task SaveAsync()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            FloorNameError = GetErrors(nameof(FloorName))
                .FirstOrDefault()?.ErrorMessage;

            if (HasErrors)
            {
                return;
            }

            bool isUpdatedSuccessfully = false;

            // 1. Perform database update and image saving inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();
                    var floorToUpdate = await db.Floors.FirstOrDefaultAsync(f => f.Id == _floorId);

                    if (floorToUpdate == null)
                    {
                        ErrorMessage = "ပြင်ဆင်လိုသော အထပ်ကို ရှာမတွေ့ပါ။";
                        return;
                    }

                    string savedImagePath = SaveImageToAppData(ImgPath, floorToUpdate.Img);

                    floorToUpdate.Name = FloorName.Trim();
                    floorToUpdate.Img = savedImagePath;
                    floorToUpdate.IsActive = IsActive;

                    await db.SaveChangesAsync();
                    isUpdatedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"ပြင်ဆင်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အထပ်အချက်အလက် ပြင်ဆင်နေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has completed and hidden the spinner
            if (isUpdatedSuccessfully)
            {
                await Back();
            }
        }

        [RelayCommand]
        public async Task Back()
        {
            var floorViewModel = new FloorViewModel(Building);

            // 1. Switch View
            WeakReferenceMessenger.Default.Send(new NavigateMessage(floorViewModel));

            // 2. Load Floors list with spinner
            await floorViewModel.LoadFloorsByBuildingIdCommand.ExecuteAsync(null);
        }
    }
}
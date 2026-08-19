using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels
{
    public partial class CreateFloorFormViewModel : FloorFormBaseViewModel
    {
        [ObservableProperty]
        private Building _building;

        public CreateFloorFormViewModel(Building building)
        {
            Building = building;
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

            bool isSavedSuccessfully = false;

            // 1. Perform image saving and database insertion inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    string savedImagePath = SaveImageToAppData(ImgPath);
                    using var db = new AppDbContext();

                    var newFloor = new Floor
                    {
                        Name = FloorName.Trim(),
                        Img = string.IsNullOrEmpty(savedImagePath) ? "pack://application:,,,/Image/DefaultImages/default_floor.png" : savedImagePath,
                        IsActive = IsActive,
                        BuildingId = Building.Id,
                    };

                    await db.Floors.AddAsync(newFloor);
                    await db.SaveChangesAsync();

                    isSavedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အထပ်အချက်အလက် သိမ်းဆည်းနေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has finished and hidden the spinner
            if (isSavedSuccessfully)
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

            // 2. Refresh Floor list with loading overlay
            await floorViewModel.LoadFloorsByBuildingIdCommand.ExecuteAsync(null);
        }
    }
}
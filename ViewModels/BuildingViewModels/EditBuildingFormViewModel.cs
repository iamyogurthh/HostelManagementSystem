using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels
{
    public partial class EditBuildingFormViewModel : BuildingFormBaseViewModel
    {
        private readonly int _buildingId;
        private readonly ImageManager _imageManager;
        private const string BuildingImagesFolder = "BuildingImages";

        public EditBuildingFormViewModel(Building building)
        {
            _buildingId = building.Id;
            _imageManager = new ImageManager();

            BuildingName = building.Name;
            IsActive = building.IsActive;

            ImgPath = building.Img ?? string.Empty;
            PreviewImageSource = !string.IsNullOrWhiteSpace(building.Img)
                ? building.Img
                : DefaultBuildingImg;
        }

        [RelayCommand]
        public override async Task SaveAsync()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            BuildingNameError = GetErrors(nameof(BuildingName))
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
                    var buildingToUpdate = await db.Buildings.FirstOrDefaultAsync(b => b.Id == _buildingId);

                    if (buildingToUpdate == null)
                    {
                        ErrorMessage = "ပြင်ဆင်လိုသော အဆောင်ကို ရှာမတွေ့ပါ။";
                        return;
                    }

                    string oldImagePath = buildingToUpdate.Img;
                    //string savedImagePath = SaveImageToAppData(ImgPath, buildingToUpdate.Img);
                    //save image using image manager
                    string savedImagePath = _imageManager.SaveImage(
                        ImgPath,
                        oldImagePath,
                        BuildingImagesFolder,
                        DefaultBuildingImg
                    );

                    buildingToUpdate.Name = BuildingName.Trim();
                    buildingToUpdate.Img = savedImagePath;
                    buildingToUpdate.IsActive = IsActive;

                    await db.SaveChangesAsync();

                    // Delete the old custom image if a new image was saved successfully
                    if (savedImagePath != oldImagePath)
                    {
                        _imageManager.DeleteImage(oldImagePath, DefaultBuildingImg);
                    }


                    isUpdatedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"ပြင်ဆင်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အဆောင်အချက်အလက် ပြင်ဆင်နေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has completed and hidden the spinner
            if (isUpdatedSuccessfully)
            {
                Back();
            }
        }
    }
}
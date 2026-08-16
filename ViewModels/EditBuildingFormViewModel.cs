using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using System;
using System.Linq;

namespace HostelManagementSystem.ViewModels
{
    public partial class EditBuildingFormViewModel : BuildingFormBaseViewModel
    {
        private readonly int _buildingId;

        public EditBuildingFormViewModel(Building building)
        {
            _buildingId = building.Id;
            BuildingName = building.Name;
            IsActive = building.IsActive;

            ImgPath = building.Img ?? string.Empty;
            PreviewImageSource = !string.IsNullOrWhiteSpace(building.Img)
                ? building.Img
                : DefaultBuildingImg;
        }

        [RelayCommand]
        public override void Save()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            BuildingNameError = GetErrors(nameof(BuildingName))
                .FirstOrDefault()?.ErrorMessage;

            if (HasErrors)
            {
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var buildingToUpdate = db.Buildings.FirstOrDefault(b => b.Id == _buildingId);

                if (buildingToUpdate == null)
                {
                    ErrorMessage = "ပြင်ဆင်လိုသော အဆောင်ကို ရှာမတွေ့ပါ။";
                    return;
                }

                string savedImagePath = SaveImageToAppData(ImgPath, buildingToUpdate.Img);

                buildingToUpdate.Name = BuildingName.Trim();
                buildingToUpdate.Img = savedImagePath;
                buildingToUpdate.IsActive = IsActive;

                db.SaveChanges();

                Back();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"ပြင်ဆင်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
        }
    }
}
using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using System;
using System.Linq;

namespace HostelManagementSystem.ViewModels
{
    public partial class CreateBuildingFormViewModel : BuildingFormBaseViewModel
    {
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
                string savedImagePath = SaveImageToAppData(ImgPath);

                using var db = new AppDbContext();

                var newBuilding = new Building
                {
                    Name = BuildingName.Trim(),
                    Img = string.IsNullOrEmpty(savedImagePath) ? "pack://application:,,,/Image/DefaultImages/default_building.png": savedImagePath,
                    IsActive = IsActive
                };

                db.Buildings.Add(newBuilding);
                db.SaveChanges();

                Back();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
        }
    }
}
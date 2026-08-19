using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels
{
    public partial class CreateBuildingFormViewModel : BuildingFormBaseViewModel
    {
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

            bool isSavedSuccessfully = false;

            // 1. Run file saving and EF Core insert inside LoadingHelper
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    string savedImagePath = SaveImageToAppData(ImgPath);

                    using var db = new AppDbContext();

                    var newBuilding = new Building
                    {
                        Name = BuildingName.Trim(),
                        Img = string.IsNullOrEmpty(savedImagePath) ? "pack://application:,,,/Image/DefaultImages/default_building.png" : savedImagePath,
                        IsActive = IsActive
                    };

                    await db.Buildings.AddAsync(newBuilding);
                    await db.SaveChangesAsync();

                    isSavedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
                }
            }, "အဆောင်အချက်အလက် သိမ်းဆည်းနေပါသည်...");

            // 2. Navigate back AFTER LoadingHelper has completed and hidden the spinner
            if (isSavedSuccessfully)
            {
                Back();
            }
        }
    }
}
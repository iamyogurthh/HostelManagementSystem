using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;


namespace HostelManagementSystem.ViewModels
{
    public partial class CreateFloorFormViewModel : FloorFormBaseViewModel
    {
        [ObservableProperty]
        private Building _building;

        public CreateFloorFormViewModel(Building building) { 
            Building = building;
        }

        [RelayCommand]
        public override void Save()
        {
            ErrorMessage = null;
            ValidateAllProperties();

            FloorNameError = GetErrors(nameof(FloorName))
                .FirstOrDefault()?.ErrorMessage;

            if(HasErrors)
            {
                return;
            }

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

                db.Floors.Add(newFloor);
                db.SaveChanges();

                Back();

            }
            catch (Exception ex)
            {
                ErrorMessage = $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Back()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new FloorViewModel(Building)));
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Messages;
using Microsoft.Win32;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace HostelManagementSystem.ViewModels
{
    public abstract partial class BuildingFormBaseViewModel : ObservableValidator
    {
        protected const string DefaultBuildingImg = "pack://application:,,,/Image/DefaultImages/default_building.png";

        [ObservableProperty]
        [Required(ErrorMessage = "အဆောင်အမည် ထည့်သွင်းရန် လိုအပ်ပါသည်")]
        [MinLength(3, ErrorMessage = "အဆောင်အမည်သည် အနည်းဆုံး ၃ လုံး ရှိရပါမည်")]
        private string _buildingName = string.Empty;

        [ObservableProperty]
        private string? _buildingNameError;

        // Stores raw selected path from file picker, or existing DB path
        [ObservableProperty]
        private string _imgPath = string.Empty;

        // Source used ONLY by the UI <Image /> control for live previewing
        [ObservableProperty]
        private string _previewImageSource = DefaultBuildingImg;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private string? _errorMessage;

        partial void OnBuildingNameChanged(string value)
        {
            ValidateProperty(value, nameof(BuildingName));

            BuildingNameError = GetErrors(nameof(BuildingName))
                .FirstOrDefault()?.ErrorMessage;
        }

        [RelayCommand]
        protected virtual void BrowseImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Building Image"
            };

            if (dialog.ShowDialog() == true)
            {
                ImgPath = dialog.FileName;
                PreviewImageSource = dialog.FileName;
            }
        }

        [RelayCommand]
        protected virtual void ClearSelectedImage()
        {
            ImgPath = DefaultBuildingImg;
            PreviewImageSource = DefaultBuildingImg;
        }

        [RelayCommand]
        protected void Back()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(new BuildingViewModel()));
        }

        // Shared helper for copying images to LocalAppData safely
        protected string SaveImageToAppData(string sourcePath, string existingDbPath = "")
        {
            // If path hasn't changed or is already set to existing AppData file, reuse it
            if (string.IsNullOrWhiteSpace(sourcePath) || sourcePath == existingDbPath)
            {
                return existingDbPath;
            }

            if (sourcePath == DefaultBuildingImg || !File.Exists(sourcePath))
            {
                return DefaultBuildingImg;
            }

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDirectory = Path.Combine(appDataFolder, "HostelManagementSystem", "BuildingImages");

            Directory.CreateDirectory(targetDirectory);

            string extension = Path.GetExtension(sourcePath);
            string uniqueFileName = $"building_{Guid.NewGuid():N}{extension}";
            string fullDestinationPath = Path.Combine(targetDirectory, uniqueFileName);

            File.Copy(sourcePath, fullDestinationPath, overwrite: true);

            return fullDestinationPath;
        }

        // Abstract save command to be implemented by child ViewModels
        public abstract Task SaveAsync();
    }
}
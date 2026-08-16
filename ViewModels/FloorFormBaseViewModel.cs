using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows;
using System.Linq;

namespace HostelManagementSystem.ViewModels
{
    public abstract partial class FloorFormBaseViewModel: ObservableValidator
    {
        protected const string DefaultFloorImg = "pack://application:,,,/Image/DefaultImages/default_floor.png";

        [ObservableProperty]
        [Required(ErrorMessage = "အထပ်အမည် ထည့်သွင်းရန် လိုအပ်ပါသည်")]
        [MinLength(3, ErrorMessage = "အထပ်အမည်သည် အနည်းဆုံး ၃ လုံး ရှိရပါမည်")]
        private string _floorName = string.Empty;

        [ObservableProperty]
        private string? _floorNameError;

        // Stores raw selected path from file picker, or existing DB path
        [ObservableProperty]
        private string _imgPath = string.Empty;

        // Source used ONLY by the UI <Image /> control for live previewing
        [ObservableProperty]
        private string _previewImageSource = DefaultFloorImg;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private string? _errorMessage;

        partial void OnFloorNameChanged(string value)
        {
            ValidateProperty(value, nameof(FloorName));

            FloorNameError = GetErrors(nameof(FloorName))
                .FirstOrDefault()?.ErrorMessage;
        }

        [RelayCommand]
        protected virtual void BrowseImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Floor Image"
            };

            if(dialog.ShowDialog() == true)
            {
                ImgPath = dialog.FileName;
                PreviewImageSource = dialog.FileName;
            }
        }

        [RelayCommand]
        protected virtual void ClearSelectedImage()
        {
            ImgPath = DefaultFloorImg;
            PreviewImageSource = DefaultFloorImg;
        }

        protected string SaveImageToAppData(string sourcePath, string existingDbPath = "")
        {
            if(string.IsNullOrWhiteSpace(sourcePath) || sourcePath == existingDbPath )
            {
                return existingDbPath;
            }

            if(sourcePath == DefaultFloorImg || !File.Exists(sourcePath))
            {
                return DefaultFloorImg;
            }

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDirectory = Path.Combine(appDataFolder, "HostelManagementSystem", "FloorImages");

            Directory.CreateDirectory(targetDirectory);

            string extension = Path.GetExtension(sourcePath);
            string uniqueFileName = $"floor_{Guid.NewGuid():N}{extension}";
            string fullDestinationPath = Path.Combine(targetDirectory, uniqueFileName);
            File.Copy(sourcePath, fullDestinationPath, overwrite: true);
        
            return fullDestinationPath;
        }

        public abstract void Save();
    }
}

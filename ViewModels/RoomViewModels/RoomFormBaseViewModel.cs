using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostelManagementSystem.ViewModels.RoomViewModels
{
    public abstract partial class RoomFormBaseViewModel : ObservableValidator
    {
        protected const string DefaultRoomImg = "pack://application:,,,/Image/DefaultImages/default_room.png";

        // အခန်းအမည် အတွက် 
        [ObservableProperty]
        [Required(ErrorMessage = "အခန်းအမည် ထည့်သွင်းရန် လိုအပ်ပါသည်")]
        [MinLength(3, ErrorMessage = "အခန်းအမည်သည် အနည်းဆုံး ၃ လုံး ရှိရပါမည်")]
        private string _roomName = string.Empty;

        [ObservableProperty]
        private string? _roomNameError;

        // အခန်း၏ လူဝင်ဆံ့နိုင်မှုပမာဏ အတွက်
        [ObservableProperty]
        [Required(ErrorMessage = "အခန်း၏ လူဝင်ဆံ့နိုင်မှုပမာဏ ထည့်သွင်းရန် လိုအပ်ပါသည်")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "လူဝင်ဆံ့မှုပမာဏသည် နံပါတ်များသာ ဖြစ်ရမည်")]
        private string _roomCapacityText = "1";

        // Helper property to get the clean integer for DB operations
        public int RoomCapacity => int.TryParse(RoomCapacityText, out var val) ? val : 0;

        [ObservableProperty]
        private string? _roomCapacityError;

        // အခန်း၏ image အတွက်
        // Stores raw selected path from file picker, or existing DB path
        [ObservableProperty]
        private string _imgPath = string.Empty;

        // Source used ONLY by the UI <Image /> control for live previewing
        [ObservableProperty]
        private string _previewImageSource = DefaultRoomImg;

        // Is it active or not
        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private string? _errorMessage;

        // name ပြောင်း handle လုပ်
        partial void OnRoomNameChanged(string value)
        {
            ValidateProperty(value, nameof(RoomName));

            RoomNameError = GetErrors(nameof(RoomName))
                .FirstOrDefault()?.ErrorMessage;
        }

        // capacity ပြောင်း handle လုပ်
        partial void OnRoomCapacityTextChanged(string value)
        {
            ValidateProperty(value, nameof(RoomCapacityText));

            // Validate Range manually for Integer bounds (1 to 1000)
            if (string.IsNullOrEmpty(GetErrors(nameof(RoomCapacityText)).FirstOrDefault()?.ErrorMessage))
            {
                if (RoomCapacity > 1000)
                {
                    RoomCapacityError = "လူဝင်ဆံ့မှုသည် ၁ မှ ၁၀၀၀ အတွင်း သာ ဖြစ်ရပါမည်";
                    return;
                }
            }

            RoomCapacityError = GetErrors(nameof(RoomCapacityText)).FirstOrDefault()?.ErrorMessage;
        }

        [RelayCommand]
        protected virtual void BrowseImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Room Image"
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
            ImgPath = DefaultRoomImg;
            PreviewImageSource = DefaultRoomImg;
        }

        public abstract Task SaveAsync();
    }
}
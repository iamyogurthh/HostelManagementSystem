using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace HostelManagementSystem.ViewModels.RoomViewModels.RoomTypeViewModels
{
    public partial class RoomTypeFormViewModel : ObservableObject
    {
        private readonly RoomTypeViewModel _previousViewModel;
        private readonly int? _roomTypeId;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        public string FormTitle =>
            IsEditMode
                ? "အခန်းအမျိုးအစားပြင်ဆင်ရန်"
                : "အခန်းအမျိုးအစားအသစ်ထည့်ရန်";

        public string SaveButtonText =>
            IsEditMode
                ? "သိမ်းဆည်းမည်"
                : "ထည့်သွင်းမည်";

        public RoomTypeFormViewModel(RoomTypeViewModel previousViewModel)
        {
            _previousViewModel = previousViewModel
                ?? throw new ArgumentNullException(nameof(previousViewModel));

            IsEditMode = false;
        }

        public RoomTypeFormViewModel(
            RoomTypeViewModel previousViewModel,
            RoomType roomType)
        {
            _previousViewModel = previousViewModel
                ?? throw new ArgumentNullException(nameof(previousViewModel));

            if (roomType == null)
                throw new ArgumentNullException(nameof(roomType));

            _roomTypeId = roomType.Id;

            Name = roomType.Name;
            Description = roomType.Description ?? string.Empty;

            IsEditMode = true;
        }

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show(
                    "အခန်းအမျိုးအစားအမည်ကို ထည့်သွင်းပေးပါ။",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Name = Name.Trim();
            Description = Description?.Trim() ?? string.Empty;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    using var db = new AppDbContext();

                    // Check duplicate name
                    var duplicateExists = await db.RoomTypes
                        .AnyAsync(rt =>
                            rt.IsActive &&
                            rt.Name.ToLower() == Name.ToLower() &&
                            (!IsEditMode || rt.Id != _roomTypeId));

                    if (duplicateExists)
                    {
                        MessageBox.Show(
                            "ဤအခန်းအမျိုးအစားအမည်ကို အသုံးပြုပြီးသားဖြစ်ပါသည်။",
                            "သတိပေးချက်",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    if (IsEditMode)
                    {
                        // Update existing Room Type
                        var roomType = await db.RoomTypes
                            .FirstOrDefaultAsync(rt => rt.Id == _roomTypeId);

                        if (roomType == null)
                        {
                            MessageBox.Show(
                                "အခန်းအမျိုးအစားကို ရှာမတွေ့ပါ။",
                                "အမှား",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            return;
                        }

                        roomType.Name = Name;
                        roomType.Description =
                            string.IsNullOrWhiteSpace(Description)
                                ? null
                                : Description;

                        await db.SaveChangesAsync();

                        WeakReferenceMessenger.Default.Send(
                            new DatabaseUpdatedMessage(
                                "RoomType",
                                roomType.Id,
                                DatabaseOperation.Updated));
                    }
                    else
                    {
                        // Create new Room Type
                        var roomType = new RoomType
                        {
                            Name = Name,
                            Description =
                                string.IsNullOrWhiteSpace(Description)
                                    ? null
                                    : Description,
                            IsActive = true
                        };

                        db.RoomTypes.Add(roomType);

                        await db.SaveChangesAsync();

                        WeakReferenceMessenger.Default.Send(
                            new DatabaseUpdatedMessage(
                                "RoomType",
                                roomType.Id,
                                DatabaseOperation.Inserted));
                    }

                    // Return to Room Type list
                    WeakReferenceMessenger.Default.Send(
                        new NavigateMessage(_previousViewModel));

                    // Reload the list
                    await _previousViewModel
                        .LoadRoomTypesCommand
                        .ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                        "အမှား",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

            }, IsEditMode
                ? "အခန်းအမျိုးအစားပြင်ဆင်နေပါသည်..."
                : "အခန်းအမျိုးအစားထည့်သွင်းနေပါသည်...");
        }

        [RelayCommand]
        private void Cancel()
        {
            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(_previousViewModel));
        }
    }
}

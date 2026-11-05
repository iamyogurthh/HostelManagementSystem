using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.RoomViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels.RoomViewModels.RoomTypeViewModels
{
    public partial class RoomTypeViewModel : ObservableObject
    {
        // Callback used to return to the ViewModel that opened this page.
        private readonly Func<Task> _navigateBackCallback;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRoomTypes))]
        [NotifyPropertyChangedFor(nameof(HasNoRoomTypes))]
        private ObservableCollection<RoomType> _roomTypes = new();

        public bool HasRoomTypes =>
            RoomTypes.Count > 0;

        public bool HasNoRoomTypes =>
            RoomTypes.Count == 0;

        public RoomTypeViewModel(
            Func<Task> navigateBackCallback)
        {
            _navigateBackCallback = navigateBackCallback
                ?? throw new ArgumentNullException(nameof(navigateBackCallback));

            RoomTypes.CollectionChanged += (s, e) =>
                NotifyStateChanges();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasRoomTypes));
            OnPropertyChanged(nameof(HasNoRoomTypes));
        }

        // ============================================================
        // LOAD ROOM TYPES
        // ============================================================

        [RelayCommand]
        public async Task LoadRoomTypesAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                await using var db = new AppDbContext();

                var roomTypeList = await db.RoomTypes
                    .AsNoTracking()
                    .Where(rt => rt.IsActive)
                    .OrderBy(rt => rt.Name)
                    .ToListAsync();

                RoomTypes.Clear();

                foreach (var roomType in roomTypeList)
                {
                    RoomTypes.Add(roomType);
                }

                NotifyStateChanges();

            }, "အခန်းအမျိုးအစားများ ရယူနေပါသည်...");
        }

        // ============================================================
        // CREATE
        // ============================================================

        [RelayCommand]
        private void OpenCreateRoomTypeForm()
        {
            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    new RoomTypeFormViewModel(this)));
        }

        // ============================================================
        // EDIT
        // ============================================================

        [RelayCommand]
        private void OpenEditRoomTypeForm(RoomType? roomType)
        {
            if (roomType == null)
                return;

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    new RoomTypeFormViewModel(
                        this,
                        roomType)));
        }

        // ============================================================
        // DELETE
        // ============================================================

        [RelayCommand]
        public async Task DeleteRoomTypeAsync(
            RoomType? roomType)
        {
            if (roomType == null)
                return;

            try
            {
                await using var db = new AppDbContext();

                // A Room Type cannot be deleted while
                // an active Room is using it.
                bool isUsed = await db.Rooms
                    .AnyAsync(r =>
                        r.RoomTypeId == roomType.Id &&
                        r.IsActive);

                if (isUsed)
                {
                    MessageBox.Show(
                        "ဤအခန်းအမျိုးအစားကို အခန်းများတွင် အသုံးပြုထားသောကြောင့် ဖျက်၍မရပါ။",
                        "သတိပေးချက်",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);

                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"စစ်ဆေးရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var result = MessageBox.Show(
                $"\"{roomType.Name}\" ကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "အခန်းအမျိုးအစားဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    await using var db = new AppDbContext();

                    var targetRoomType =
                        await db.RoomTypes
                            .FirstOrDefaultAsync(rt =>
                                rt.Id == roomType.Id);

                    if (targetRoomType == null)
                        return;

                    // Soft delete
                    targetRoomType.IsActive = false;

                    await db.SaveChangesAsync();

                    RoomTypes.Remove(roomType);

                    WeakReferenceMessenger.Default.Send(
                        new DatabaseUpdatedMessage(
                            "RoomType",
                            roomType.Id,
                            DatabaseOperation.Deleted));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"ဖျက်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                        "အမှား",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

            }, "အခန်းအမျိုးအစားဖျက်နေပါသည်...");
        }

        // ============================================================
        // BACK
        // ============================================================

        [RelayCommand]
        private async Task BackAsync()
        {
            await _navigateBackCallback();
        }
    }
}
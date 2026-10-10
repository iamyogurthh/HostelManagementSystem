using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace HostelManagementSystem.ViewModels.RoomAssignmentViewModels
{
    public partial class RoomAssignmentViewModel : ObservableObject
    {
        private readonly StudentAccommodationService _accommodationService;
        private readonly int _studentId;
        private readonly Func<Task>? _navigateBackCallback;

        // =========================================================
        // STUDENT
        // =========================================================

        [ObservableProperty]
        private Student? _student;

        // =========================================================
        // CURRENT ACCOMMODATION
        // =========================================================

        [ObservableProperty]
        private StudentAccommodation? _currentAccommodation;

        [ObservableProperty]
        private string _currentRoomText = "အခန်းမရှိပါ";

        // =========================================================
        // BUILDINGS
        // =========================================================

        [ObservableProperty]
        private ObservableCollection<Building> _availableBuildings = new();

        [ObservableProperty]
        private Building? _selectedBuilding;

        // =========================================================
        // FLOORS
        // =========================================================

        [ObservableProperty]
        private ObservableCollection<Floor> _availableFloors = new();

        [ObservableProperty]
        private Floor? _selectedFloor;

        // =========================================================
        // ROOMS
        // =========================================================

        [ObservableProperty]
        private ObservableCollection<Room> _availableRooms = new();
         
        [ObservableProperty]
        private Room? _selectedRoom;

        // =========================================================
        // ASSIGNMENT DATE
        // =========================================================

        [ObservableProperty]
        private DateTime _assignmentDate = DateTime.Today;

        // =========================================================
        // STATE
        // =========================================================

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasCurrentRoom =>
            CurrentAccommodation != null;

        public bool IsChangingRoom =>
            CurrentAccommodation != null;

        public string ActionButtonText =>
            IsChangingRoom
                ? "အခန်းပြောင်းမည်"
                : "အခန်းသတ်မှတ်မည်";

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RoomAssignmentViewModel(
            int studentId,
            Func<Task>? navigateBackCallback = null)
        {
            _studentId = studentId;
            _navigateBackCallback = navigateBackCallback;

            _accommodationService =
                new StudentAccommodationService();

            _ = InitializeAsync();
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await LoadStudentAsync();
                await LoadBuildingsAsync();
                await LoadCurrentAccommodationAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အခန်းအချက်အလက်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================
        // LOAD STUDENT
        // =========================================================

        private async Task LoadStudentAsync()
        {
            await using var db = new AppDbContext();

            Student = await db.Students
                .AsNoTracking()
                .Include(s => s.StudentCategory)
                .FirstOrDefaultAsync(s =>
                    s.Id == _studentId &&
                    s.IsActive);

            if (Student == null)
            {
                throw new InvalidOperationException(
                    "Student does not exist or is inactive.");
            }
        }

        // =========================================================
        // LOAD BUILDINGS
        // =========================================================

        private async Task LoadBuildingsAsync()
        {
            await using var db = new AppDbContext();

            var buildings = await db.Buildings
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            AvailableBuildings =
                new ObservableCollection<Building>(buildings);
        }

        // =========================================================
        // LOAD CURRENT ACCOMMODATION
        // =========================================================

        private async Task LoadCurrentAccommodationAsync()
        {
            CurrentAccommodation =
                await _accommodationService
                    .GetCurrentAsync(_studentId);

            if (CurrentAccommodation?.Room != null)
            {
                var room = CurrentAccommodation.Room;
                var floor = room.Floor;
                var building = floor?.Building;

                CurrentRoomText =
                    building != null && floor != null
                        ? $"{building.Name} / {floor.Name} / {room.Name}"
                        : room.Name;

                if (building != null)
                {
                    SelectedBuilding =
                        AvailableBuildings.FirstOrDefault(
                            b => b.Id == building.Id);
                }
            }
            else
            {
                CurrentRoomText = "အခန်းမရှိပါ";
            }

            OnPropertyChanged(nameof(HasCurrentRoom));
            OnPropertyChanged(nameof(IsChangingRoom));
            OnPropertyChanged(nameof(ActionButtonText));
        }

        // =========================================================
        // BUILDING CHANGED
        // =========================================================

        partial void OnSelectedBuildingChanged(
            Building? value)
        {
            _ = LoadFloorsAsync(value);
        }

        private async Task LoadFloorsAsync(
            Building? building)
        {
            AvailableFloors.Clear();
            AvailableRooms.Clear();

            SelectedFloor = null;
            SelectedRoom = null;

            if (building == null)
            {
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                var floors = await db.Floors
                    .AsNoTracking()
                    .Where(f =>
                        f.BuildingId == building.Id &&
                        f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                AvailableFloors =
                    new ObservableCollection<Floor>(floors);

                var currentFloorId =
                    CurrentAccommodation?.Room?.FloorId;

                if (currentFloorId.HasValue &&
                    building.Id ==
                    CurrentAccommodation?.Room?.Floor?.BuildingId)
                {
                    SelectedFloor =
                        AvailableFloors.FirstOrDefault(
                            f => f.Id == currentFloorId.Value);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အထပ်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
        }

        // =========================================================
        // FLOOR CHANGED
        // =========================================================

        partial void OnSelectedFloorChanged(
            Floor? value)
        {
            _ = LoadRoomsAsync(value);
        }

        private async Task LoadRoomsAsync(
            Floor? floor)
        {
            AvailableRooms.Clear();
            SelectedRoom = null;

            if (floor == null)
            {
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                var rooms = await db.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Where(r =>
                    r.FloorId == floor.Id &&
                    r.IsActive &&
                    r.Accommodations.Count(a =>
                        a.IsActive &&
                        a.EndDate == null) < r.Capacity)
                .OrderBy(r => r.Name)
                .ToListAsync();

                AvailableRooms =
                    new ObservableCollection<Room>(rooms);

                var currentRoomId =
                    CurrentAccommodation?.RoomId;

                if (currentRoomId.HasValue &&
                    floor.Id ==
                    CurrentAccommodation?.Room?.FloorId)
                {
                    SelectedRoom =
                        AvailableRooms.FirstOrDefault(
                            r => r.Id == currentRoomId.Value);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အခန်းများ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
        }

        // =========================================================
        // ASSIGN / CHANGE ROOM
        // =========================================================

        [RelayCommand]
        private async Task AssignRoomAsync()
        {
            ErrorMessage = string.Empty;

            if (Student == null)
            {
                ErrorMessage =
                    "ကျောင်းသားအချက်အလက် မတွေ့ပါ။";

                return;
            }

            if (SelectedRoom == null)
            {
                ErrorMessage =
                    "အခန်းတစ်ခန်း ရွေးချယ်ပေးပါ။";

                return;
            }

            if (AssignmentDate.Date < DateTime.Today)
            {
                ErrorMessage =
                    "အခန်းသတ်မှတ်မည့်ရက်သည် ယနေ့မတိုင်မီ ဖြစ်၍မရပါ။";

                return;
            }

            var isSameRoom =
                CurrentAccommodation != null &&
                CurrentAccommodation.RoomId ==
                SelectedRoom.Id;

            if (isSameRoom)
            {
                MessageBox.Show(
                    "ကျောင်းသားသည် ဤအခန်းတွင် ရှိပြီးသားဖြစ်ပါသည်။",
                    "အချက်အလက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var result = MessageBox.Show(
                $"{Student.Name} ကို " +
                $"{SelectedRoom.Name} သို့ " +
                $"{(IsChangingRoom ? "အခန်းပြောင်း" : "အခန်းသတ်မှတ်")} " +
                "မည်မှာ သေချာပါသလား?",
                IsChangingRoom
                    ? "အခန်းပြောင်းခြင်း"
                    : "အခန်းသတ်မှတ်ခြင်း",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                IsLoading = true;

                var startDate =
                    DateOnly.FromDateTime(AssignmentDate.Date);

                await _accommodationService.AssignAsync(
                    _studentId,
                    SelectedRoom.Id,
                    startDate);

                WeakReferenceMessenger.Default.Send(
                    new DatabaseUpdatedMessage(
                        "StudentAccommodation",
                        _studentId,
                        DatabaseOperation.Updated));

                MessageBox.Show(
                    IsChangingRoom
                        ? "အခန်းပြောင်းပြီးပါပြီ။"
                        : "အခန်းသတ်မှတ်ပြီးပါပြီ။",
                    "အောင်မြင်ပါသည်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (_navigateBackCallback != null)
                {
                    await _navigateBackCallback();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အခန်းသတ်မှတ်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";

                MessageBox.Show(
                    ErrorMessage,
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }


        // =========================================================
        // Leave Room
        // =========================================================
        [RelayCommand]
        private async Task ReleaseAccommodationAsync()
        {
            ErrorMessage = string.Empty;

            if (CurrentAccommodation == null)
            {
                ErrorMessage = "လက်ရှိအခန်းသတ်မှတ်ထားခြင်း မရှိပါ။";
                return;
            }

            var departureDate =
                DateOnly.FromDateTime(AssignmentDate.Date);

            if (departureDate < CurrentAccommodation.StartDate)
            {
                ErrorMessage =
                    "ထွက်ခွာမည့်ရက်သည် အခန်းစတင်နေထိုင်သည့်ရက်ထက် စော၍မရပါ။";
                return;
            }

            var result = MessageBox.Show(
                $"{Student?.Name} သည် အဆောင်မှ ထွက်ခွာမည်မှာ သေချာပါသလား?",
                "အဆောင်မှ ထွက်ခွာခြင်း",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                await _accommodationService.ReleaseAsync(
                    _studentId,
                    departureDate);

                WeakReferenceMessenger.Default.Send(
                    new DatabaseUpdatedMessage(
                        "StudentAccommodation",
                        _studentId,
                        DatabaseOperation.Updated));

                MessageBox.Show(
                    "အဆောင်မှ ထွက်ခွာမှုကို မှတ်တမ်းတင်ပြီးပါပြီ။",
                    "အောင်မြင်ပါသည်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (_navigateBackCallback != null)
                    await _navigateBackCallback();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"ထွက်ခွာမှု မှတ်တမ်းတင်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        // =========================================================
        // CANCEL
        // =========================================================

        [RelayCommand]
        private async Task CancelAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback();
            }
        }
    }
}

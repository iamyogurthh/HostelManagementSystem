using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.Base;
using HostelManagementSystem.ViewModels.RoomAssignmentViewModels;
using HostelManagementSystem.ViewModels.StudentCategoryViewModels;
using HostelManagementSystem.ViewModels.StudentDetailViewModels;
using HostelManagementSystem.ViewModels.StudentViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels
{
    public partial class ResidentViewModel :
        PaginationViewModel
    {
        // =========================================================
        // SEARCH
        // =========================================================

        [ObservableProperty]
        private string _searchName = string.Empty;

        // =========================================================
        // DATA
        // =========================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStudents))]
        [NotifyPropertyChangedFor(nameof(HasNoStudents))]
        private ObservableCollection<Student> _students =
            new();

        [ObservableProperty]
        private bool _isLoading;

        public bool HasStudents =>
            Students.Count > 0;

        public bool HasNoStudents =>
            !HasStudents;

        // =========================================================
        // LOCATION FILTERS
        // =========================================================

        public ObservableCollection<Building>
            AvailableBuildings
        { get; } = new();

        public ObservableCollection<Floor>
            AvailableFloors
        { get; } = new();

        public ObservableCollection<Room>
            AvailableRooms
        { get; } = new();

        [ObservableProperty]
        private Building? _selectedBuilding;

        [ObservableProperty]
        private Floor? _selectedFloor;

        [ObservableProperty]
        private Room? _selectedRoom;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ResidentViewModel()
        {
            WeakReferenceMessenger.Default.Register<DatabaseUpdatedMessage>(
                this,
                async (recipient, message) =>
                {
                    if (message.EntityName is
                        "Building" or
                        "Floor" or
                        "Room" or
                        "RoomType" or
                        "Student" or
                        "StudentAccommodation")
                    {
                        await RefreshLocationDataAsync();
                    }
                });

            _ = InitializeViewModelAsync();
        }

        // =========================================================
        // INITIALIZATION
        // =========================================================

        private async Task InitializeViewModelAsync()
        {
            await LoadBuildingsAsync();

            await LoadDataAsync();
        }

        private async Task RefreshLocationDataAsync()
        {
            try
            {
                SelectedRoom = null;
                SelectedFloor = null;
                SelectedBuilding = null;

                AvailableRooms.Clear();
                AvailableFloors.Clear();
                AvailableBuildings.Clear();

                await LoadBuildingsAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to refresh resident data: {ex}");
            }
        }

        // =========================================================
        // CASCADING FILTERS
        // =========================================================

        partial void OnSelectedBuildingChanged(
            Building? value)
        {
            SelectedFloor = null;

            AvailableFloors.Clear();

            SelectedRoom = null;

            AvailableRooms.Clear();

            if (value != null)
            {
                _ =
                    LoadFloorsForBuildingAsync(value);
            }
        }

        partial void OnSelectedFloorChanged(
            Floor? value)
        {
            SelectedRoom = null;

            AvailableRooms.Clear();

            if (value != null)
            {
                _ =
                    LoadRoomsForFloorAsync(value);
            }
        }

        // =========================================================
        // LOAD BUILDINGS
        // =========================================================

        private async Task LoadBuildingsAsync()
        {
            try
            {
                await using var db =
                    new AppDbContext();

                var buildings =
                    await db.Buildings
                        .AsNoTracking()
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.Name)
                        .ToListAsync();

                Application.Current
                    .Dispatcher
                    .Invoke(() =>
                    {
                        AvailableBuildings.Clear();

                        foreach (var building
                            in buildings)
                        {
                            AvailableBuildings.Add(
                                building);
                        }
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load buildings: {ex.Message}");
            }
        }

        // =========================================================
        // LOAD FLOORS
        // =========================================================

        private async Task LoadFloorsForBuildingAsync(
            Building building)
        {
            try
            {
                await using var db =
                    new AppDbContext();

                var floors =
                    await db.Floors
                        .Where(f =>
                            f.BuildingId ==
                                building.Id &&
                            f.IsActive)
                        .AsNoTracking()
                        .OrderBy(f => f.Name)
                        .ToListAsync();

                Application.Current
                    .Dispatcher
                    .Invoke(() =>
                    {
                        AvailableFloors.Clear();

                        foreach (var floor
                            in floors)
                        {
                            AvailableFloors.Add(
                                floor);
                        }
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load floors: {ex.Message}");
            }
        }

        // =========================================================
        // LOAD ROOMS
        // =========================================================

        private async Task LoadRoomsForFloorAsync(
            Floor floor)
        {
            try
            {
                await using var db =
                    new AppDbContext();

                var rooms =
                    await db.Rooms
                        .Where(r =>
                            r.FloorId ==
                                floor.Id &&
                            r.IsActive)
                        .AsNoTracking()
                        .OrderBy(r => r.Name)
                        .ToListAsync();

                Application.Current
                    .Dispatcher
                    .Invoke(() =>
                    {
                        AvailableRooms.Clear();

                        foreach (var room
                            in rooms)
                        {
                            AvailableRooms.Add(
                                room);
                        }
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load rooms: {ex.Message}");
            }
        }

        // =========================================================
        // APPLY FILTERS
        // =========================================================

        [RelayCommand]
        private async Task ApplyFilterAndReloadAsync()
        {
            CurrentPage = 1;

            await LoadDataAsync();
        }

        // =========================================================
        // RESET FILTERS
        // =========================================================

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            SearchName = string.Empty;

            SelectedBuilding = null;

            SelectedFloor = null;

            SelectedRoom = null;

            AvailableFloors.Clear();

            AvailableRooms.Clear();

            CurrentPage = 1;

            await LoadDataAsync();
        }

        // =========================================================
        // LOAD STUDENTS
        // =========================================================

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;

                await using var db =
                    new AppDbContext();

                IQueryable<Student> query =
                    db.Students
                        .AsNoTracking()
                        .Where(s => s.IsActive);

                // =================================================
                // TEXT SEARCH
                // =================================================

                if (!string.IsNullOrWhiteSpace(
                    SearchName))
                {
                    string term =
                        SearchName.Trim();

                    query =
                        query.Where(s =>
                            (s.Name != null &&
                             EF.Functions.Like(
                                 s.Name,
                                 $"%{term}%")) ||

                            (s.NRC != null &&
                             EF.Functions.Like(
                                 s.NRC,
                                 $"%{term}%")));
                }

                // =================================================
                // LOCATION FILTER
                // =================================================

                if (SelectedRoom != null)
                {
                    int roomId =
                        SelectedRoom.Id;

                    query =
                        query.Where(s =>
                            s.Accommodations.Any(
                                a =>
                                    a.IsActive &&
                                    a.EndDate == null &&
                                    a.RoomId == roomId));
                }
                else if (SelectedFloor != null)
                {
                    int floorId =
                        SelectedFloor.Id;

                    query =
                        query.Where(s =>
                            s.Accommodations.Any(
                                a =>
                                    a.IsActive &&
                                    a.EndDate == null &&
                                    a.Room.FloorId ==
                                        floorId));
                }
                else if (SelectedBuilding != null)
                {
                    int buildingId =
                        SelectedBuilding.Id;

                    query =
                        query.Where(s =>
                            s.Accommodations.Any(
                                a =>
                                    a.IsActive &&
                                    a.EndDate == null &&
                                    a.Room.Floor.BuildingId ==
                                        buildingId));
                }

                // =================================================
                // PAGINATION
                // =================================================

                var totalCount =
                    await query.CountAsync();

                UpdatePagination(totalCount);

                // =================================================
                // CURRENT PAGE
                // =================================================

                var items =
                    await query
                        .OrderByDescending(
                            s => s.CreatedAt)

                        .Skip(GetSkipCount())

                        .Take(PageSize)

                        .Include(s =>
                            s.Accommodations
                                .Where(a =>
                                    a.IsActive &&
                                    a.EndDate == null))
                            .ThenInclude(a => a.Room)
                                .ThenInclude(r =>
                                    r.Floor)
                                    .ThenInclude(f =>
                                        f.Building)

                        .Include(s =>
                            s.AcademicRecords)

                        .Include(s =>
                            s.StudentCategory)

                        .ToListAsync();

                Students =
                    new ObservableCollection<Student>(
                        items);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"အချက်အလက်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
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
        // PAGINATION REFRESH
        // =========================================================

        protected override async Task RefreshPageAsync()
        {
            await LoadDataAsync();
        }

        // =========================================================
        // STUDENT CATEGORY
        // =========================================================

        [RelayCommand]
        private async Task OpenStudentCategoryManagement()
        {
            var studentCategoryViewModel =
                new StudentCategoryViewModel(
                    async () =>
                    {
                        await LoadDataAsync();

                        WeakReferenceMessenger
                            .Default
                            .Send(
                                new NavigateMessage(
                                    this));
                    });

            WeakReferenceMessenger
                .Default
                .Send(
                    new NavigateMessage(
                        studentCategoryViewModel));

            await studentCategoryViewModel
                .LoadCategoriesCommand
                .ExecuteAsync(null);
        }

        // =========================================================
        // ADD STUDENT
        // =========================================================

        [RelayCommand]
        private void OpenAddNewStudentForm()
        {
            Func<Task>
                navigateBackCallback = () =>
                {
                    WeakReferenceMessenger
                        .Default
                        .Send(
                            new NavigateMessage(
                                new ResidentViewModel()));

                    return Task.CompletedTask;
                };

            var addVm =
                new AddNewStudentFormViewModel(
                    navigateBackCallback);

            WeakReferenceMessenger
                .Default
                .Send(
                    new NavigateMessage(
                        addVm));
        }

        // =========================================================
        // STUDENT DETAIL
        // =========================================================

        [RelayCommand]
        private void NavigateToStudentDetail(
            Student student)
        {
            if (student == null)
            {
                return;
            }

            Func<Task>
                navigateBackCallback = () =>
                {
                    WeakReferenceMessenger
                        .Default
                        .Send(
                            new NavigateMessage(
                                new ResidentViewModel()));

                    return Task.CompletedTask;
                };

            var detailVm =
                new StudentDetailViewModel(
                    student.Id,
                    navigateBackCallback);

            WeakReferenceMessenger
                .Default
                .Send(
                    new NavigateMessage(
                        detailVm));
        }

        // =========================================================
        // EDIT STUDENT
        // =========================================================

        [RelayCommand]
        private void EditStudent(
            Student student)
        {
            if (student == null)
            {
                return;
            }

            Func<Task>
                navigateBackCallback = () =>
                {
                    WeakReferenceMessenger
                        .Default
                        .Send(
                            new NavigateMessage(
                                new ResidentViewModel()));

                    return Task.CompletedTask;
                };

            var editVm =
                new EditStudentFormViewModel(
                    student.Id,
                    navigateBackCallback);

            WeakReferenceMessenger
                .Default
                .Send(
                    new NavigateMessage(
                        editVm));
        }

        
        // =========================================================
        // ASSIGN / CHANGE ROOM
        // =========================================================

        [RelayCommand]
        private void AssignRoom(int studentId)
        {
                    if (studentId <= 0)
                    {
                        return;
                    }

                    Func<Task> navigateBackCallback = async () =>
                    {
                        // Refresh the existing ResidentViewModel
                        // so current room information is immediately updated.
                        await LoadDataAsync();

                        WeakReferenceMessenger.Default.Send(
                            new NavigateMessage(this));
                    };

                    var roomAssignmentVm =
                        new RoomAssignmentViewModel(
                            studentId,
                            navigateBackCallback);

                    WeakReferenceMessenger.Default.Send(
                        new NavigateMessage(
                            roomAssignmentVm));
        }



        // =========================================================
        // DELETE STUDENT
        // =========================================================

        [RelayCommand]
        private async Task DeleteStudentAsync(
            int studentId)
        {
            var result =
                MessageBox.Show(
                    "ဤအဆောင်သားကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                    "ဖျက်ရန် အတည်ပြုပါ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }

            await LoadingHelper
                .ExecuteWithLoadingAsync(
                    async () =>
                    {
                        try
                        {
                            var studentService =
                                new StudentService();

                            await studentService
                                .DeleteStudentWithRelatedDataAsync(
                                    studentId,
                                    DateOnly.FromDateTime(
                                        DateTime.Today));

                            await LoadDataAsync();

                            WeakReferenceMessenger
                                .Default
                                .Send(
                                    new DatabaseUpdatedMessage(
                                        "Student",
                                        studentId,
                                        DatabaseOperation
                                            .Deleted));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"ဖျက်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                                "အမှား",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    },
                    "အချက်အလက်များ ဖျက်နေပါသည်...");
        }
    }
}
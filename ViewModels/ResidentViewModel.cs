using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.StudentDetailViewModels;
using HostelManagementSystem.ViewModels.StudentViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels
{
    public partial class ResidentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchName = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<int> PageSizeOptions { get; } = new() { 10, 20, 50 };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStudents))]
        [NotifyPropertyChangedFor(nameof(HasNoStudents))]
        private ObservableCollection<Student> _students = new();

        public bool HasStudents => Students != null && Students.Count > 0;
        public bool HasNoStudents => !HasStudents;

        // ==========================================
        // Cascading Selection Collections & Properties
        // ==========================================
        public ObservableCollection<Building> AvailableBuildings { get; } = new();
        public ObservableCollection<Floor> AvailableFloors { get; } = new();
        public ObservableCollection<Room> AvailableRooms { get; } = new();

        [ObservableProperty]
        private Building? _selectedBuilding;

        [ObservableProperty]
        private Floor? _selectedFloor;

        [ObservableProperty]
        private Room? _selectedRoom;

        public ResidentViewModel()
        {
            _ = InitializeViewModelAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            await LoadBuildingsAsync();
            await LoadDataAsync();
        }

        #region Cascading Property Changes

        partial void OnSelectedBuildingChanged(Building? value)
        {
            SelectedFloor = null;
            AvailableFloors.Clear();
            SelectedRoom = null;
            AvailableRooms.Clear();

            if (value != null)
            {
                _ = LoadFloorsForBuildingAsync(value);
            }
        }

        partial void OnSelectedFloorChanged(Floor? value)
        {
            SelectedRoom = null;
            AvailableRooms.Clear();

            if (value != null)
            {
                _ = LoadRoomsForFloorAsync(value);
            }
        }

        #endregion

        #region Cascading Data Loaders

        private async Task LoadBuildingsAsync()
        {
            try
            {
                await using var db = new AppDbContext();
                var buildings = await db.Buildings.AsNoTracking().ToListAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    AvailableBuildings.Clear();
                    foreach (var b in buildings)
                    {
                        AvailableBuildings.Add(b);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load buildings: {ex.Message}");
            }
        }

        private async Task LoadFloorsForBuildingAsync(Building building)
        {
            try
            {
                await using var db = new AppDbContext();
                var floors = await db.Floors
                    .Where(f => f.BuildingId == building.Id)
                    .AsNoTracking()
                    .ToListAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    AvailableFloors.Clear();
                    foreach (var f in floors)
                    {
                        AvailableFloors.Add(f);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load floors: {ex.Message}");
            }
        }

        private async Task LoadRoomsForFloorAsync(Floor floor)
        {
            try
            {
                await using var db = new AppDbContext();
                var rooms = await db.Rooms
                    .Where(r => r.FloorId == floor.Id)
                    .AsNoTracking()
                    .ToListAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    AvailableRooms.Clear();
                    foreach (var r in rooms)
                    {
                        AvailableRooms.Add(r);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load rooms: {ex.Message}");
            }
        }

        #endregion

        partial void OnPageSizeChanged(int value)
        {
            CurrentPage = 1;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task ApplyFilterAndReloadAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            SearchName = string.Empty;
            SelectedBuilding = null; // Clears Floor and Room via OnSelectedBuildingChanged
            CurrentPage = 1;
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                using var db = new AppDbContext();

                IQueryable<Student> query = db.Students.AsNoTracking();

                // 1. Text Search Filter (Name / NRC)
                if (!string.IsNullOrWhiteSpace(SearchName))
                {
                    string term = SearchName.Trim();
                    query = query.Where(s =>
                        (s.Name != null && EF.Functions.Like(s.Name, $"%{term}%")) ||
                        (s.NRC != null && EF.Functions.Like(s.NRC, $"%{term}%")));
                }

                // 2. Cascading Location Filters
                if (SelectedRoom != null)
                {
                    // Specific Room Selected
                    query = query.Where(s => s.RoomId == SelectedRoom.Id);
                }
                else if (SelectedFloor != null)
                {
                    // Specific Floor Selected (Matches all rooms on this floor)
                    query = query.Where(s => s.Room != null && s.Room.FloorId == SelectedFloor.Id);
                }
                else if (SelectedBuilding != null)
                {
                    // Specific Building Selected (Matches all rooms in this building)
                    query = query.Where(s => s.Room != null && s.Room.Floor != null && s.Room.Floor.BuildingId == SelectedBuilding.Id);
                }

                TotalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                if (TotalPages < 1) TotalPages = 1;

                var items = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Include(s => s.Room)
                        .ThenInclude(r => r!.Floor)
                            .ThenInclude(f => f!.Building)
                    .Include(s => s.AcademicRecords)
                    .ToListAsync();

                Students = new ObservableCollection<Student>(items);
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

        #region Pagination Commands

        [RelayCommand]
        private async Task FirstPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task LastPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                await LoadDataAsync();
            }
        }

        #endregion

        #region Actions & Navigation

        [RelayCommand]
        private void OpenAddNewStudentForm()
        {
            Func<Task> navigateBackCallback = () =>
            {
                WeakReferenceMessenger.Default.Send(new NavigateMessage(new ResidentViewModel()));
                return Task.CompletedTask;
            };

            var addVm = new AddNewStudentFormViewModel(navigateBackCallback);
            WeakReferenceMessenger.Default.Send(new NavigateMessage(addVm));
        }

        [RelayCommand]
        private void NavigateToStudentDetail(Student student)
        {
            if (student == null) return;

            var room = student.Room;
            var floor = room?.Floor;
            var building = floor?.Building;

            if (building == null || floor == null || room == null)
            {
                MessageBox.Show(
                    "ကျောင်းသား၏ အခန်း အချက်အလက်များ မပြည့်စုံပါ၊",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Func<Task> navigateBackCallback = () =>
            {
                WeakReferenceMessenger.Default.Send(new NavigateMessage(new ResidentViewModel()));
                return Task.CompletedTask;
            };

            var detailVm = new StudentDetailViewModel(
                building,
                floor,
                room,
                student.Id,
                navigateBackCallback
            );

            WeakReferenceMessenger.Default.Send(new NavigateMessage(detailVm));
        }

        [RelayCommand]
        private void EditStudent(Student student)
        {
            if (student == null) return;

            var room = student.Room;
            var floor = room?.Floor;
            var building = floor?.Building;

            if (building == null || floor == null || room == null)
            {
                MessageBox.Show(
                    "ကျောင်းသား၏ အခန်း အချက်အလက်များ မပြည့်စုံပါ၊",
                    "သတိပေးချက်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Func<Task> navigateBackCallback = () =>
            {
                WeakReferenceMessenger.Default.Send(new NavigateMessage(new ResidentViewModel()));
                return Task.CompletedTask;
            };

            var editVm = new EditStudentFormViewModel(
                building,
                floor,
                room,
                student.Id,
                navigateBackCallback
            );

            WeakReferenceMessenger.Default.Send(new NavigateMessage(editVm));
        }

        [RelayCommand]
        private async Task DeleteStudentAsync(int studentId)
        {
            var result = MessageBox.Show(
                "ဤအဆောင်သားကို အမှန်တကယ် ဖျက်လိုပါသလား။",
                "ဖျက်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    var studentService = new StudentService();
                    await studentService.DeleteStudentWithRelatedDataAsync(studentId);

                    await LoadDataAsync();

                    WeakReferenceMessenger.Default.Send(new DatabaseUpdatedMessage("Student", studentId, DatabaseOperation.Deleted));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"ဖျက်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                        "အမှား",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }, "အချက်အလက်များ ဖျက်နေပါသည်...");
        }

        #endregion
    }
}
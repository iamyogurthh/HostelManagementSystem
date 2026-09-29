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

        public ResidentViewModel()
        {
            _ = LoadDataAsync();
        }

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

                if (!string.IsNullOrWhiteSpace(SearchName))
                {
                    string term = SearchName.Trim();
                    query = query.Where(s =>
                        (s.Name != null && EF.Functions.Like(s.Name, $"%{term}%")) ||
                        (s.NRC != null && EF.Functions.Like(s.NRC, $"%{term}%")));
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
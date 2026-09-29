using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.RoomDetailViewModels;
using HostelManagementSystem.ViewModels.StudentViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels.StudentDetailViewModels
{
    public partial class StudentDetailViewModel : ObservableObject
    {
        private readonly Func<Task>? _navigateBackCallback;

        // Navigation Context
        [ObservableProperty]
        private Building _building = null!;

        [ObservableProperty]
        private Floor _floor = null!;

        [ObservableProperty]
        private Room _room = null!;

        // Student Data Properties
        [ObservableProperty]
        private Student? _student;

        [ObservableProperty]
        private ObservableCollection<StudentAcademicRecord> _academicRecords = new();

        // Image Path Helpers
        [ObservableProperty]
        private string? _profileImagePath;

        [ObservableProperty]
        private string? _nrcFrontImagePath;

        [ObservableProperty]
        private string? _nrcBackImagePath;

        [ObservableProperty]
        private string? _highSchoolCertImagePath;

        // Image Preview Properties
        [ObservableProperty]
        private bool _isImagePreviewOpen;

        [ObservableProperty]
        private string? _previewImagePath;

        // =========================================================
        // CONSTRUCTORS
        // =========================================================
        public StudentDetailViewModel(
            Building building,
            Floor floor,
            Room room,
            int studentId,
            Func<Task>? navigateBackCallback = null)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));
            Room = room ?? throw new ArgumentNullException(nameof(room));
            _navigateBackCallback = navigateBackCallback;

            // Automatically trigger data loading on navigation
            _ = LoadStudentDetailsByStudentIdCommand.ExecuteAsync(studentId);
        }

        // Default Constructor for Design-Time / XAML support
        public StudentDetailViewModel()
        {
        }

        // =========================================================
        // DATA FETCHING COMMAND
        // =========================================================
        [RelayCommand]
        public async Task LoadStudentDetailsByStudentIdAsync(int studentId)
        {
            if (studentId <= 0) return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                await using var db = new AppDbContext();

                var studentData = await db.Students
                    .AsNoTracking()
                    .Include(s => s.AcademicRecords)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (studentData != null)
                {
                    Student = studentData;
                    AcademicRecords = new ObservableCollection<StudentAcademicRecord>(studentData.AcademicRecords);

                    // Image Paths Resolution
                    ProfileImagePath = ResolveImagePath(studentData.Img);
                    NrcFrontImagePath = ResolveImagePath(studentData.NRC_img_front);
                    NrcBackImagePath = ResolveImagePath(studentData.NRC_img_back);
                    HighSchoolCertImagePath = ResolveImagePath(studentData.High_school_certificate_img);
                }
            }, "ကျောင်းသား အချက်အလက်များ ရယူနေပါသည်...");
        }

        private string? ResolveImagePath(string? relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
                return null;

            if (relativeOrAbsolutePath.StartsWith("pack://") || File.Exists(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeOrAbsolutePath);
            return File.Exists(fullPath) ? fullPath : null;
        }

        // =========================================================
        // ACTION & NAVIGATION COMMANDS
        // =========================================================
        [RelayCommand]
        private void OpenImagePreview(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            PreviewImagePath = imagePath;
            IsImagePreviewOpen = true;
        }

        [RelayCommand]
        private void CloseImagePreview()
        {
            IsImagePreviewOpen = false;
            PreviewImagePath = null;
        }

        [RelayCommand]
        private void EditStudent()
        {
            if (Student == null || Student.Id <= 0) return;

            var editViewModel = new EditStudentFormViewModel(
                Building, Floor, Room, Student.Id,
                navigateBackCallback: async () =>
                {
                    var studentDetailVm = new StudentDetailViewModel(Building, Floor, Room, Student.Id, _navigateBackCallback);
                    WeakReferenceMessenger.Default.Send(new NavigateMessage(studentDetailVm));
                    if (studentDetailVm.LoadStudentDetailsByStudentIdCommand.CanExecute(Student.Id))
                    {
                        await studentDetailVm.LoadStudentDetailsByStudentIdCommand.ExecuteAsync(Student.Id);
                    }
                });

            WeakReferenceMessenger.Default.Send(new NavigateMessage(editViewModel));
        }

        [RelayCommand]
        private async Task DeleteStudentAsync()
        {
            if (Student == null || Student.Id <= 0) return;

            var result = MessageBox.Show(
                $"ကျောင်းသား '{Student.Name}' ၏ အချက်အလက်များကို ဖျက်ရန် သေချာပါသလား။",
                "ကျောင်းသား အချက်အလက် ဖျက်ရန် အတည်ပြုခြင်း",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    var studentService = new StudentService(); // Or inject via DI
                    await studentService.DeleteStudentWithRelatedDataAsync(Student.Id);

                    // Update the Dashboard
                    WeakReferenceMessenger.Default.Send(new DatabaseUpdatedMessage("Student", Student.Id, DatabaseOperation.Deleted));

                    // Navigate back after successful deletion
                    await App.Current.Dispatcher.InvokeAsync(async () => await BackAsync());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ဖျက်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, "ကျောင်းသား အချက်အလက် ဖျက်နေပါသည်...");
        }

        // =========================================================
        // NAVIGATION COMMANDS
        // =========================================================
        [RelayCommand]
        private async Task BackAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
            }
            else
            {
                var roomDetailViewModel = new RoomDetailViewModel(Building, Floor, Room);
                WeakReferenceMessenger.Default.Send(new NavigateMessage(roomDetailViewModel));
            }
        }
    }
}
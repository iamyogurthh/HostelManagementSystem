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
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        // =========================================================
        // NAVIGATION CONTEXT
        // =========================================================

        [ObservableProperty]
        private Building? _building;

        [ObservableProperty]
        private Floor? _floor;

        [ObservableProperty]
        private Room? _room;

        // =========================================================
        // STUDENT DATA
        // =========================================================

        [ObservableProperty]
        private Student? _student;

        [ObservableProperty]
        private ObservableCollection<StudentAcademicRecord> _academicRecords = new();

        // =========================================================
        // IMAGE PATH HELPERS
        // =========================================================

        [ObservableProperty]
        private string? _profileImagePath;

        [ObservableProperty]
        private string? _nrcFrontImagePath;

        [ObservableProperty]
        private string? _nrcBackImagePath;

        [ObservableProperty]
        private string? _highSchoolCertImagePath;

        // =========================================================
        // IMAGE PREVIEW
        // =========================================================

        [ObservableProperty]
        private bool _isImagePreviewOpen;

        [ObservableProperty]
        private string? _previewImagePath;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public StudentDetailViewModel(
            int studentId,
            Func<Task>? navigateBackCallback = null)
        {
            _navigateBackCallback = navigateBackCallback;

            _ = LoadStudentDetailsByStudentIdCommand
                .ExecuteAsync(studentId);
        }

        // Default constructor for design-time / XAML support
        public StudentDetailViewModel()
        {
        }

        // =========================================================
        // DATA FETCHING
        // =========================================================

        [RelayCommand]
        public async Task LoadStudentDetailsByStudentIdAsync(
            int studentId)
        {
            if (studentId <= 0)
                return;

            await LoadingHelper.ExecuteWithLoadingAsync(
                async () =>
                {
                    await using var db =
                        new AppDbContext();

                    var studentData =
                        await db.Students
                            .AsNoTracking()
                            .Include(s => s.AcademicRecords)
                            .Include(s => s.StudentCategory)
                            .Include(s => s.Accommodations
                                .Where(a =>
                                    a.IsActive &&
                                    a.EndDate == null))
                                .ThenInclude(a => a.Room)
                                    .ThenInclude(r => r.Floor)
                                        .ThenInclude(f => f.Building)
                            .FirstOrDefaultAsync(
                                s =>
                                    s.Id == studentId &&
                                    s.IsActive);

                    if (studentData == null)
                    {
                        MessageBox.Show(
                            "ကျောင်းသားအချက်အလက်ကို ရှာမတွေ့ပါ။",
                            "သတိပေးချက်",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    Student = studentData;

                    AcademicRecords =
                        new ObservableCollection<StudentAcademicRecord>(
                            studentData.AcademicRecords);

                    // =================================================
                    // CURRENT ACCOMMODATION
                    // =================================================

                    var accommodation =
                        studentData.Accommodations
                            .FirstOrDefault(a =>
                                a.IsActive &&
                                a.EndDate == null);

                    if (accommodation != null)
                    {
                        Room = accommodation.Room;
                        Floor = accommodation.Room?.Floor;
                        Building = accommodation.Room?.Floor?.Building;
                    }
                    else
                    {
                        Room = null;
                        Floor = null;
                        Building = null;
                    }

                    // =================================================
                    // IMAGE PATHS
                    // =================================================

                    ProfileImagePath =
                        ResolveImagePath(studentData.Img);

                    NrcFrontImagePath =
                        ResolveImagePath(
                            studentData.NRC_img_front);

                    NrcBackImagePath =
                        ResolveImagePath(
                            studentData.NRC_img_back);

                    HighSchoolCertImagePath =
                        ResolveImagePath(
                            studentData.High_school_certificate_img);
                },
                "ကျောင်းသား အချက်အလက်များ ရယူနေပါသည်...");
        }

        // =========================================================
        // IMAGE PATH
        // =========================================================

        private string? ResolveImagePath(
            string? relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(
                relativeOrAbsolutePath))
            {
                return null;
            }

            if (relativeOrAbsolutePath.StartsWith(
                    "pack://",
                    StringComparison.OrdinalIgnoreCase) ||
                File.Exists(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            string fullPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    relativeOrAbsolutePath);

            return File.Exists(fullPath)
                ? fullPath
                : null;
        }

        // =========================================================
        // IMAGE PREVIEW
        // =========================================================

        [RelayCommand]
        private void OpenImagePreview(
            string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            PreviewImagePath = imagePath;
            IsImagePreviewOpen = true;
        }

        [RelayCommand]
        private void CloseImagePreview()
        {
            IsImagePreviewOpen = false;
            PreviewImagePath = null;
        }

        // =========================================================
        // EDIT STUDENT
        // =========================================================

        [RelayCommand]
        private void EditStudent()
        {
            if (Student == null ||
                Student.Id <= 0)
            {
                return;
            }

            var studentId = Student.Id;

            var editViewModel =
                new EditStudentFormViewModel(
                    studentId,
                    navigateBackCallback: async () =>
                    {
                        var studentDetailVm =
                            new StudentDetailViewModel(
                                studentId,
                                _navigateBackCallback);

                        WeakReferenceMessenger.Default.Send(
                            new NavigateMessage(
                                studentDetailVm));

                        await Task.CompletedTask;
                    });

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    editViewModel));
        }

        // =========================================================
        // DELETE STUDENT
        // =========================================================

        [RelayCommand]
        private async Task DeleteStudentAsync()
        {
            if (Student == null ||
                Student.Id <= 0)
            {
                return;
            }

            var result =
                MessageBox.Show(
                    $"ကျောင်းသား '{Student.Name}' ၏ အချက်အလက်များကို ဖျက်ရန် သေချာပါသလား။",
                    "ကျောင်းသား အချက်အလက် ဖျက်ရန် အတည်ပြုခြင်း",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await LoadingHelper.ExecuteWithLoadingAsync(
                async () =>
                {
                    try
                    {
                        var studentService =
                            new StudentService();

                        await studentService
                            .DeleteStudentWithRelatedDataAsync(
                                Student.Id,
                                DateOnly.FromDateTime(DateTime.Today));

                        WeakReferenceMessenger.Default.Send(
                            new DatabaseUpdatedMessage(
                                "Student",
                                Student.Id,
                                DatabaseOperation.Deleted));

                        await BackAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"ဖျက်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                            "Database Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                },
                "ကျောင်းသား အချက်အလက် ဖျက်နေပါသည်...");
        }

        // =========================================================
        // EXPORT STUDENT
        // =========================================================

        [RelayCommand]
        private async Task ExportStudentAsync()
        {
            if (Student == null)
                return;

            try
            {
                var exportPDFService =
                    new ExportPDFService();

                await exportPDFService
                    .ExportStudentAsPdfAsync(
                        Student,
                        ProfileImagePath!,
                        Building,
                        Floor,
                        Room);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"PDF သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // NAVIGATION
        // =========================================================

        [RelayCommand]
        private async Task BackAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
                return;
            }

            // No room context means there is nowhere to return
            // to RoomDetail. Fall back to ResidentView.

            if (Building == null ||
                Floor == null ||
                Room == null)
            {
                WeakReferenceMessenger.Default.Send(
                    new NavigateMessage(
                        new ResidentViewModel()));

                return;
            }

            var roomDetailViewModel =
                new RoomDetailViewModel(
                    Building,
                    Floor,
                    Room);

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    roomDetailViewModel));
        }
    }
}
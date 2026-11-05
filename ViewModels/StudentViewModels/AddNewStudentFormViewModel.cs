using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.DTOs;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HostelManagementSystem.ViewModels.StudentViewModels
{
    public partial class AddNewStudentFormViewModel : ObservableObject
    {
        private readonly ImageManager _imageManager =
            new ImageManager();

        private readonly Func<Task>? _navigateBackCallback;

        // ============================================================
        // Student Category
        // ============================================================

        [ObservableProperty]
        private ObservableCollection<StudentCategory>
            _availableStudentCategories = new();

        [ObservableProperty]
        private StudentCategory? _selectedStudentCategory;

        [ObservableProperty]
        private string _studentCategoryError = string.Empty;

        // ============================================================
        // Student
        // ============================================================

        // ============================================================
        // Sub-ViewModels
        // ============================================================

        public NrcInputViewModel StudentNrc { get; }

        public NrcInputViewModel FatherNrc { get; }

        public NrcInputViewModel MotherNrc { get; }

        public BirthdayInputViewModel StudentDateOfBirth { get; }

        public MajorInputViewModel StudentMajor { get; }

        // ============================================================
        // Form Properties
        // ============================================================

        [ObservableProperty]
        private string _studentName = string.Empty;

        [ObservableProperty]
        private string _fatherName = string.Empty;

        [ObservableProperty]
        private string _motherName = string.Empty;

        [ObservableProperty]
        private string _studentPh = string.Empty;

        [ObservableProperty]
        private string _parentPh = string.Empty;

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _selectedGender = string.Empty;

        // ============================================================
        // High School
        // ============================================================

        [ObservableProperty]
        private string? _highSchoolRollNo = string.Empty;

        [ObservableProperty]
        private string? _highSchoolName = string.Empty;

        [ObservableProperty]
        private string? _highSchoolCity = string.Empty;

        [ObservableProperty]
        private bool _isSaving;

        // ============================================================
        // Academic Records
        // ============================================================

        public ObservableCollection<AcademicRecordInputViewModel>
            AcademicRecords
        { get; } = new();

        public List<string> YearLevels { get; } = new()
        {
            "ပထမနှစ်",
            "ဒုတိယနှစ်",
            "တတိယနှစ်",
            "စတုတ္ထနှစ်",
            "ပဉ္စမနှစ်",
            "ပထမနှစ်-ဂုဏ်ထူးတန်း",
            "ဒုတိယနှစ်-ဂုဏ်ထူးတန်း",
            "တတိယနှစ်-ဂုဏ်ထူးတန်း",
            "မဟာတန်း"
        };

        public List<int> AttemptNumbers { get; } = new()
        {
            1,
            2,
            3
        };

        // ============================================================
        // Images
        // ============================================================

        [ObservableProperty]
        private ImageSource? _studentPhotoPreviewSource;

        private string _studentPhotoPath = string.Empty;

        [ObservableProperty]
        private ImageSource? _highSchoolCertPreviewSource;

        private string _highSchoolCertPath = string.Empty;

        [ObservableProperty]
        private ImageSource? _nrcFrontPreviewSource;

        private string _nrcFrontImagePath = string.Empty;

        [ObservableProperty]
        private ImageSource? _nrcBackPreviewSource;

        private string _nrcBackImagePath = string.Empty;

        // ============================================================
        // Constructor
        // ============================================================

        public AddNewStudentFormViewModel(
            Func<Task>? navigateBackCallback = null)
            : this(
                NrcManager.Instance,
                navigateBackCallback)
        {
        }

        public AddNewStudentFormViewModel(
            NrcManager nrcManager,
            Func<Task>? navigateBackCallback = null)
        {
            _navigateBackCallback =
                navigateBackCallback;

            StudentNrc =
                new NrcInputViewModel(nrcManager);

            FatherNrc =
                new NrcInputViewModel(nrcManager);

            MotherNrc =
                new NrcInputViewModel(nrcManager);

            StudentDateOfBirth =
                new BirthdayInputViewModel();

            StudentMajor =
                new MajorInputViewModel();

            _ = LoadStudentCategoriesAsync();
        }

        // ============================================================
        // Student Categories
        // ============================================================

        private async Task LoadStudentCategoriesAsync()
        {
            try
            {
                await using var db =
                    new AppDbContext();

                var categories =
                    await db.StudentCategories
                        .AsNoTracking()
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableStudentCategories.Clear();

                    foreach (var category in categories)
                    {
                        AvailableStudentCategories.Add(category);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ကျောင်းသားအမျိုးအစားများ ရယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ============================================================
        // Student Category Validation
        // ============================================================

        private bool ValidateStudentCategory()
        {
            if (SelectedStudentCategory == null)
            {
                StudentCategoryError =
                    "ကျောင်းသား/သူ အမျိုးအစား ရွေးချယ်ပါ။";

                return false;
            }

            StudentCategoryError =
                string.Empty;

            return true;
        }

        // ============================================================
        // Save Student
        // ============================================================

        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsSaving)
            {
                return;
            }

            // ========================================================
            // Validation
            // ========================================================

            bool isStudentNrcValid =
                StudentNrc.Validate("Student");

            bool isFatherNrcValid =
                FatherNrc.Validate("Father");

            bool isMotherNrcValid =
                MotherNrc.Validate("Mother");

            bool isDobValid =
                StudentDateOfBirth.Validate(
                    "Date of Birth");

            bool isMajorValid =
                StudentMajor.Validate("Major");

            bool isStudentCategoryValid =
                ValidateStudentCategory();

            if (!isStudentNrcValid ||
                !isFatherNrcValid ||
                !isMotherNrcValid ||
                !isDobValid ||
                !isMajorValid ||
                !isStudentCategoryValid)
            {
                MessageBox.Show(
                    "ကျေးဇူးပြု၍ လိုအပ်သော အချက်အလက်များကို မှန်ကန်စွာ ဖြည့်သွင်းပါ။",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                IsSaving = true;

                // ====================================================
                // Save Images
                // ====================================================

                string profilePhotoPath = string.Empty;

                string highSchoolCertPath = string.Empty;

                string nrcFrontImagePath = string.Empty;

                string nrcBackImagePath = string.Empty;

                await Task.Run(() =>
                {
                    profilePhotoPath =
                        _imageManager.SaveImage(
                            _studentPhotoPath,
                            string.Empty,
                            "StudentPhotos");

                    highSchoolCertPath =
                        _imageManager.SaveImage(
                            _highSchoolCertPath,
                            string.Empty,
                            "Certificates");

                    nrcFrontImagePath =
                        _imageManager.SaveImage(
                            _nrcFrontImagePath,
                            string.Empty,
                            "NrcImages");

                    nrcBackImagePath =
                        _imageManager.SaveImage(
                            _nrcBackImagePath,
                            string.Empty,
                            "NrcImages");
                });

                // ====================================================
                // Create DTO
                // ====================================================

                var dto = new CreateStudentDto
                {
                    StudentCategoryId =
                        SelectedStudentCategory!.Id,

                    Name =
                        StudentName?.Trim()
                        ?? string.Empty,

                    Gender =
                        SelectedGender?.Trim()
                        ?? string.Empty,

                    Birthday =
                        StudentDateOfBirth.SelectedDate
                        ?? DateTime.MinValue,

                    NRC =
                        StudentNrc.FullNrc,

                    Student_ph =
                        StudentPh?.Trim()
                        ?? string.Empty,

                    Parent_ph =
                        ParentPh?.Trim()
                        ?? string.Empty,

                    Address =
                        Address?.Trim()
                        ?? string.Empty,

                    Father_name =
                        FatherName?.Trim()
                        ?? string.Empty,

                    Mother_name =
                        MotherName?.Trim()
                        ?? string.Empty,

                    Father_NRC =
                        FatherNrc.FullNrc,

                    Mother_NRC =
                        MotherNrc.FullNrc,

                    Major =
                        StudentMajor.SelectedMajor,

                    High_school_roll_no =
                        HighSchoolRollNo?.Trim()
                        ?? string.Empty,

                    High_school_name =
                        HighSchoolName?.Trim()
                        ?? string.Empty,

                    High_school_city =
                        HighSchoolCity?.Trim()
                        ?? string.Empty,

                    Img =
                        profilePhotoPath,

                    High_school_certificate_img =
                        highSchoolCertPath,

                    NRC_img_front =
                        nrcFrontImagePath,

                    NRC_img_back =
                        nrcBackImagePath,

                    AcademicRecords =
                        AcademicRecords
                            .Select(r =>
                                new CreateStudentAcademicRecordDto
                                {
                                    AcademicYearLevel =
                                        r.AcademicYearLevel,

                                    AttemptNumber =
                                        r.AttemptNumber,

                                    RollNumber =
                                        r.RollNumber?.Trim()
                                        ?? string.Empty,

                                    AttendedYear =
                                        r.AttendedYear?.Trim()
                                        ?? string.Empty,

                                    IsHonours =
                                        r.IsHonours
                                })
                            .ToList()
                };

                // ====================================================
                // Save Database
                // ====================================================

                await using var db =
                    new AppDbContext();

                await using var transaction =
                    await db.Database
                        .BeginTransactionAsync();

                try
                {
                    var student = new Student
                    {
                        StudentCategoryId =
                            dto.StudentCategoryId,

                        Name =
                            dto.Name,

                        Gender =
                            dto.Gender,

                        Birthday =
                            dto.Birthday,

                        NRC =
                            dto.NRC,

                        Student_ph =
                            dto.Student_ph,

                        Parent_ph =
                            dto.Parent_ph,

                        Address =
                            dto.Address,

                        Father_name =
                            dto.Father_name,

                        Mother_name =
                            dto.Mother_name,

                        Father_NRC =
                            dto.Father_NRC,

                        Mother_NRC =
                            dto.Mother_NRC,

                        Major =
                            dto.Major,

                        High_school_roll_no =
                            dto.High_school_roll_no,

                        High_school_name =
                            dto.High_school_name,

                        High_school_city =
                            dto.High_school_city,

                        Img =
                            dto.Img,

                        High_school_certificate_img =
                            dto.High_school_certificate_img,

                        NRC_img_front =
                            dto.NRC_img_front,

                        NRC_img_back =
                            dto.NRC_img_back,

                        IsActive = true,

                        CreatedAt =
                            DateTime.Now,

                        UpdatedAt =
                            DateTime.Now
                    };

                    foreach (var academicRecord
                        in dto.AcademicRecords)
                    {
                        student.AcademicRecords.Add(
                            new StudentAcademicRecord
                            {
                                AcademicYearLevel =
                                    academicRecord
                                        .AcademicYearLevel,

                                AttemptNumber =
                                    academicRecord
                                        .AttemptNumber,

                                RollNumber =
                                    academicRecord
                                        .RollNumber
                                    ?? string.Empty,

                                AttendedYear =
                                    academicRecord
                                        .AttendedYear
                                    ?? string.Empty,

                                IsHonours =
                                    academicRecord
                                        .IsHonours
                            });
                    }

                    await db.Students.AddAsync(student);

                    // IMPORTANT:
                    // No StudentAccommodation is created here.
                    // Room assignment is handled separately.

                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    WeakReferenceMessenger.Default.Send(
                        new DatabaseUpdatedMessage(
                            "Student",
                            student.Id,
                            DatabaseOperation.Inserted));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // ====================================================
                // Success
                // ====================================================

                MessageBox.Show(
                    "ကျောင်းသား အချက်အလက်များကို အောင်မြင်စွာ သိမ်းဆည်းပြီးပါပြီ။",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ကျောင်းသား အချက်အလက်သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        // ============================================================
        // Cancel
        // ============================================================

        [RelayCommand]
        private async Task CancelAsync()
        {
            await NavigateBackAsync();
        }

        // ============================================================
        // Export Blank Form Template
        // ============================================================

        [RelayCommand]
        private async Task ExportFormTemplateAsync()
        {
            try
            {
                var exportPdfService =
                    new ExportPDFService();

                await exportPdfService
                    .ExportFormTemplateAsPdfAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ဖောင်ပုံစံ PDF ထုတ်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                    "PDF Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task NavigateBackAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
            }
        }

        // ============================================================
        // Image Commands
        // ============================================================

        [RelayCommand]
        private void BrowseStudentPhoto()
        {
            string? path =
                SelectImageFileDialog(
                    "Select Student Photo");

            if (path != null)
            {
                _studentPhotoPath = path;

                StudentPhotoPreviewSource =
                    LoadBitmapImageSafely(path);
            }
        }

        [RelayCommand]
        private void ClearStudentPhoto()
        {
            _studentPhotoPath = string.Empty;

            StudentPhotoPreviewSource = null;
        }

        [RelayCommand]
        private void BrowseHighSchoolCert()
        {
            string? path =
                SelectImageFileDialog(
                    "Select Certificate Image");

            if (path != null)
            {
                _highSchoolCertPath = path;

                HighSchoolCertPreviewSource =
                    LoadBitmapImageSafely(path);
            }
        }

        [RelayCommand]
        private void ClearHighSchoolCert()
        {
            _highSchoolCertPath = string.Empty;

            HighSchoolCertPreviewSource = null;
        }

        [RelayCommand]
        private void BrowseNrcFront()
        {
            string? path =
                SelectImageFileDialog(
                    "Select NRC Front");

            if (path != null)
            {
                _nrcFrontImagePath = path;

                NrcFrontPreviewSource =
                    LoadBitmapImageSafely(path);
            }
        }

        [RelayCommand]
        private void ClearNrcFront()
        {
            _nrcFrontImagePath = string.Empty;

            NrcFrontPreviewSource = null;
        }

        [RelayCommand]
        private void BrowseNrcBack()
        {
            string? path =
                SelectImageFileDialog(
                    "Select NRC Back");

            if (path != null)
            {
                _nrcBackImagePath = path;

                NrcBackPreviewSource =
                    LoadBitmapImageSafely(path);
            }
        }

        [RelayCommand]
        private void ClearNrcBack()
        {
            _nrcBackImagePath = string.Empty;

            NrcBackPreviewSource = null;
        }

        // ============================================================
        // Academic Records
        // ============================================================

        [RelayCommand]
        private void AddAcademicRecord()
        {
            AcademicRecords.Add(
                new AcademicRecordInputViewModel());
        }

        [RelayCommand]
        private void RemoveAcademicRecord(
            AcademicRecordInputViewModel? record)
        {
            if (record != null)
            {
                AcademicRecords.Remove(record);
            }
        }

        // ============================================================
        // Image Dialog
        // ============================================================

        private static string? SelectImageFileDialog(
            string title)
        {
            var dialog =
                new OpenFileDialog
                {
                    Title = title,

                    Filter =
                        "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|" +
                        "All Files (*.*)|*.*"
                };

            return dialog.ShowDialog() == true
                ? dialog.FileName
                : null;
        }

        // ============================================================
        // Safe Bitmap Loading
        // ============================================================

        private static BitmapImage?
            LoadBitmapImageSafely(
                string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                return null;
            }

            try
            {
                byte[] bytes =
                    File.ReadAllBytes(filePath);

                using var ms =
                    new MemoryStream(bytes);

                var bitmap =
                    new BitmapImage();

                bitmap.BeginInit();

                bitmap.CacheOption =
                    BitmapCacheOption.OnLoad;

                bitmap.StreamSource = ms;

                bitmap.EndInit();

                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
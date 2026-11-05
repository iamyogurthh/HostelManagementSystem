using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.DTOs;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
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
    public partial class EditStudentFormViewModel :
        ObservableObject
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

        public int StudentId { get; }

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

        [ObservableProperty]
        private bool _isLoading = true;

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
        // Existing Paths
        // ============================================================

        public string ExistingProfilePhotoPath { get; set; } =
            string.Empty;

        public string ExistingHighSchoolCertPath { get; set; } =
            string.Empty;

        public string ExistingNrcFrontImagePath { get; set; } =
            string.Empty;

        public string ExistingNrcBackImagePath { get; set; } =
            string.Empty;

        // ============================================================
        // Constructors
        // ============================================================

        public EditStudentFormViewModel(
            NrcManager nrcManager,
            int studentId,
            Func<Task>? navigateBackCallback = null)
        {
            StudentId = studentId;

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

            _ = InitializeFormAsync();
        }

        public EditStudentFormViewModel(
            int studentId,
            Func<Task>? navigateBackCallback = null)
            : this(
                NrcManager.Instance,
                studentId,
                navigateBackCallback)
        {
        }

        // ============================================================
        // Initialize
        // ============================================================

        private async Task InitializeFormAsync()
        {
            await LoadStudentCategoriesAsync();

            await LoadStudentDetailsAsync();
        }

        // ============================================================
        // Load Categories
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
        // Load Student
        // ============================================================

        private async Task LoadStudentDetailsAsync()
        {
            try
            {
                IsLoading = true;

                Student? student;

                await using (var db =
                    new AppDbContext())
                {
                    student =
                        await db.Students
                            .AsNoTracking()
                            .Include(s =>
                                s.AcademicRecords)
                            .FirstOrDefaultAsync(
                                s => s.Id == StudentId);
                }

                if (student == null)
                {
                    MessageBox.Show(
                        "ကျောင်းသား အချက်အလက် ရှာမတွေ့ပါ။",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    await NavigateBackAsync();

                    return;
                }

                // ====================================================
                // Personal Information
                // ====================================================

                StudentName =
                    student.Name ??
                    string.Empty;

                FatherName =
                    student.Father_name ??
                    string.Empty;

                MotherName =
                    student.Mother_name ??
                    string.Empty;

                StudentPh =
                    student.Student_ph ??
                    string.Empty;

                ParentPh =
                    student.Parent_ph ??
                    string.Empty;

                Address =
                    student.Address ??
                    string.Empty;

                SelectedGender =
                    student.Gender ??
                    string.Empty;

                // ====================================================
                // Student Category
                // ====================================================

                SelectedStudentCategory =
                    AvailableStudentCategories
                        .FirstOrDefault(
                            category =>
                                category.Id ==
                                student.StudentCategoryId);

                StudentCategoryError =
                    string.Empty;

                // ====================================================
                // High School
                // ====================================================

                HighSchoolRollNo =
                    student.High_school_roll_no ??
                    string.Empty;

                HighSchoolName =
                    student.High_school_name ??
                    string.Empty;

                HighSchoolCity =
                    student.High_school_city ??
                    string.Empty;

                // ====================================================
                // Sub-ViewModels
                // ====================================================

                StudentDateOfBirth.SetDate(
                    student.Birthday);

                StudentMajor.SelectedMajor =
                    student.Major;

                StudentNrc.SetNrc(
                    student.NRC);

                FatherNrc.SetNrc(
                    student.Father_NRC);

                MotherNrc.SetNrc(
                    student.Mother_NRC);

                // ====================================================
                // Existing Images
                // ====================================================

                ExistingProfilePhotoPath =
                    student.Img ??
                    string.Empty;

                ExistingHighSchoolCertPath =
                    student.High_school_certificate_img ??
                    string.Empty;

                ExistingNrcFrontImagePath =
                    student.NRC_img_front ??
                    string.Empty;

                ExistingNrcBackImagePath =
                    student.NRC_img_back ??
                    string.Empty;

                _studentPhotoPath =
                    ExistingProfilePhotoPath;

                StudentPhotoPreviewSource =
                    LoadBitmapImageSafely(
                        _studentPhotoPath);

                _highSchoolCertPath =
                    ExistingHighSchoolCertPath;

                HighSchoolCertPreviewSource =
                    LoadBitmapImageSafely(
                        _highSchoolCertPath);

                _nrcFrontImagePath =
                    ExistingNrcFrontImagePath;

                NrcFrontPreviewSource =
                    LoadBitmapImageSafely(
                        _nrcFrontImagePath);

                _nrcBackImagePath =
                    ExistingNrcBackImagePath;

                NrcBackPreviewSource =
                    LoadBitmapImageSafely(
                        _nrcBackImagePath);

                // ====================================================
                // Academic Records
                // ====================================================

                AcademicRecords.Clear();

                foreach (var record
                    in student.AcademicRecords)
                {
                    AcademicRecords.Add(
                        new AcademicRecordInputViewModel
                        {
                            AcademicYearLevel =
                                record.AcademicYearLevel,

                            AttemptNumber =
                                record.AttemptNumber,

                            RollNumber =
                                record.RollNumber ??
                                string.Empty,

                            AttendedYear =
                                record.AttendedYear ??
                                string.Empty,

                            IsHonours =
                                record.IsHonours
                        });
                }

                OnPropertyChanged(
                    nameof(StudentNrc));

                OnPropertyChanged(
                    nameof(FatherNrc));

                OnPropertyChanged(
                    nameof(MotherNrc));

                OnPropertyChanged(
                    nameof(StudentDateOfBirth));

                OnPropertyChanged(
                    nameof(StudentMajor));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"အချက်အလက်များ ဆွဲယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ============================================================
        // Validation
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
        // Save
        // ============================================================

        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsSaving)
            {
                return;
            }

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

                string profilePhotoPath =
                    string.Empty;

                string highSchoolCertPath =
                    string.Empty;

                string nrcFrontImagePath =
                    string.Empty;

                string nrcBackImagePath =
                    string.Empty;

                await Task.Run(() =>
                {
                    profilePhotoPath =
                        _imageManager.SaveImage(
                            _studentPhotoPath,
                            ExistingProfilePhotoPath,
                            "StudentPhotos");

                    highSchoolCertPath =
                        _imageManager.SaveImage(
                            _highSchoolCertPath,
                            ExistingHighSchoolCertPath,
                            "Certificates");

                    nrcFrontImagePath =
                        _imageManager.SaveImage(
                            _nrcFrontImagePath,
                            ExistingNrcFrontImagePath,
                            "NrcImages");

                    nrcBackImagePath =
                        _imageManager.SaveImage(
                            _nrcBackImagePath,
                            ExistingNrcBackImagePath,
                            "NrcImages");
                });

                var dto =
                    new UpdateStudentDto
                    {
                        Id = StudentId,

                        StudentCategoryId =
                            SelectedStudentCategory!.Id,

                        Name =
                            StudentName?.Trim()
                            ?? string.Empty,

                        Gender =
                            SelectedGender ??
                            string.Empty,

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
                                    new UpdateStudentAcademicRecordDto
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
                // Database
                // ====================================================

                await using var db =
                    new AppDbContext();

                await using var transaction =
                    await db.Database
                        .BeginTransactionAsync();

                try
                {
                    var existingStudent =
                        await db.Students
                            .Include(s =>
                                s.AcademicRecords)
                            .FirstOrDefaultAsync(
                                s => s.Id == dto.Id);

                    if (existingStudent == null)
                    {
                        throw new InvalidOperationException(
                            "Student could not be found.");
                    }

                    // =================================================
                    // Delete replaced images
                    // =================================================

                    if (existingStudent.Img != dto.Img &&
                        !string.IsNullOrWhiteSpace(
                            existingStudent.Img))
                    {
                        _imageManager.DeleteImage(
                            existingStudent.Img);
                    }

                    if (
                        existingStudent
                            .High_school_certificate_img !=
                        dto.High_school_certificate_img &&
                        !string.IsNullOrWhiteSpace(
                            existingStudent
                                .High_school_certificate_img))
                    {
                        _imageManager.DeleteImage(
                            existingStudent
                                .High_school_certificate_img);
                    }

                    if (existingStudent.NRC_img_front !=
                        dto.NRC_img_front &&
                        !string.IsNullOrWhiteSpace(
                            existingStudent.NRC_img_front))
                    {
                        _imageManager.DeleteImage(
                            existingStudent.NRC_img_front);
                    }

                    if (existingStudent.NRC_img_back !=
                        dto.NRC_img_back &&
                        !string.IsNullOrWhiteSpace(
                            existingStudent.NRC_img_back))
                    {
                        _imageManager.DeleteImage(
                            existingStudent.NRC_img_back);
                    }

                    // =================================================
                    // Update Student
                    // =================================================

                    existingStudent.StudentCategoryId =
                        dto.StudentCategoryId;

                    existingStudent.Name =
                        dto.Name;

                    existingStudent.Gender =
                        dto.Gender;

                    existingStudent.Birthday =
                        dto.Birthday;

                    existingStudent.NRC =
                        dto.NRC;

                    existingStudent.Student_ph =
                        dto.Student_ph;

                    existingStudent.Parent_ph =
                        dto.Parent_ph;

                    existingStudent.Address =
                        dto.Address;

                    existingStudent.Father_name =
                        dto.Father_name;

                    existingStudent.Mother_name =
                        dto.Mother_name;

                    existingStudent.Father_NRC =
                        dto.Father_NRC;

                    existingStudent.Mother_NRC =
                        dto.Mother_NRC;

                    existingStudent.Major =
                        dto.Major;

                    existingStudent.High_school_roll_no =
                        dto.High_school_roll_no;

                    existingStudent.High_school_name =
                        dto.High_school_name;

                    existingStudent.High_school_city =
                        dto.High_school_city;

                    existingStudent.Img =
                        dto.Img;

                    existingStudent
                        .High_school_certificate_img =
                        dto.High_school_certificate_img;

                    existingStudent.NRC_img_front =
                        dto.NRC_img_front;

                    existingStudent.NRC_img_back =
                        dto.NRC_img_back;

                    existingStudent.UpdatedAt =
                        DateTime.Now;

                    // =================================================
                    // Replace Academic Records
                    // =================================================

                    existingStudent.AcademicRecords.Clear();

                    foreach (var record
                        in dto.AcademicRecords)
                    {
                        existingStudent.AcademicRecords.Add(
                            new StudentAcademicRecord
                            {
                                StudentId =
                                    dto.Id,

                                AcademicYearLevel =
                                    record.AcademicYearLevel,

                                AttemptNumber =
                                    record.AttemptNumber,

                                RollNumber =
                                    record.RollNumber ??
                                    string.Empty,

                                AttendedYear =
                                    record.AttendedYear ??
                                    string.Empty,

                                IsHonours =
                                    record.IsHonours
                            });
                    }

                    // IMPORTANT:
                    // No room/accommodation changes here.

                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    WeakReferenceMessenger.Default.Send(
                        new DatabaseUpdatedMessage(
                            "Student",
                            StudentId,
                            DatabaseOperation.Updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                MessageBox.Show(
                    "ကျောင်းသား အချက်အလက်များကို ပြင်ဆင်ပြီးပါပြီ။",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ဒေတာပြင်ဆင်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
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

        private async Task NavigateBackAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
            }
        }

        // ============================================================
        // Images
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
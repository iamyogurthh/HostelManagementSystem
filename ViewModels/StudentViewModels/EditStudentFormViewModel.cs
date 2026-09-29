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
    public partial class EditStudentFormViewModel : ObservableObject
    {
        private readonly ImageManager _imageManager = new ImageManager();

        // Dynamic callback action for returning back
        private readonly Func<Task>? _navigateBackCallback;

        // Cascading Selection Collections
        public ObservableCollection<Building> AvailableBuildings { get; } = new();
        public ObservableCollection<Floor> AvailableFloors { get; } = new();
        public ObservableCollection<Room> AvailableRooms { get; } = new();

        [ObservableProperty] private Building? _selectedBuilding;
        [ObservableProperty] private Floor? _selectedFloor;
        [ObservableProperty] private Room? _room;

        [ObservableProperty] private bool _isRoomSelectionEnabled = true;

        public int StudentId { get; }

        // Sub-ViewModels
        public NrcInputViewModel StudentNrc { get; }
        public NrcInputViewModel FatherNrc { get; }
        public NrcInputViewModel MotherNrc { get; }
        public BirthdayInputViewModel StudentDateOfBirth { get; }
        public MajorInputViewModel StudentMajor { get; }

        // Form Properties
        [ObservableProperty] private string _studentName = string.Empty;
        [ObservableProperty] private string _fatherName = string.Empty;
        [ObservableProperty] private string _motherName = string.Empty;
        [ObservableProperty] private string _studentPh = string.Empty;
        [ObservableProperty] private string _parentPh = string.Empty;
        [ObservableProperty] private string _address = string.Empty;
        [ObservableProperty] private string _selectedGender = string.Empty;

        // Nullable High School fields
        [ObservableProperty] private string? _highSchoolRollNo = string.Empty;
        [ObservableProperty] private string? _highSchoolName = string.Empty;
        [ObservableProperty] private string? _highSchoolCity = string.Empty;

        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private bool _isLoading = true;

        public ObservableCollection<AcademicRecordInputViewModel> AcademicRecords { get; } = new();

        public List<string> YearLevels { get; } = new()
        {
            "ပထမနှစ်", "ဒုတိယနှစ်", "တတိယနှစ်", "စတုတ္ထနှစ်", "ပဉ္စမနှစ်",
            "ပထမနှစ်-ဂုဏ်ထူးတန်း", "ဒုတိယနှစ်-ဂုဏ်ထူးတန်း", "တတိယနှစ်-ဂုဏ်ထူးတန်း", "မဟာတန်း"
        };
        public List<int> AttemptNumbers { get; } = new() { 1, 2, 3 };

        // Images Preview Controls & Local Paths
        [ObservableProperty] private ImageSource? _studentPhotoPreviewSource;
        private string _studentPhotoPath = string.Empty;

        [ObservableProperty] private ImageSource? _highSchoolCertPreviewSource;
        private string _highSchoolCertPath = string.Empty;

        [ObservableProperty] private ImageSource? _nrcFrontPreviewSource;
        private string _nrcFrontImagePath = string.Empty;

        [ObservableProperty] private ImageSource? _nrcBackPreviewSource;
        private string _nrcBackImagePath = string.Empty;

        // Existing Paths loaded from DB
        public string ExistingProfilePhotoPath { get; set; } = string.Empty;
        public string ExistingHighSchoolCertPath { get; set; } = string.Empty;
        public string ExistingNrcFrontImagePath { get; set; } = string.Empty;
        public string ExistingNrcBackImagePath { get; set; } = string.Empty;

        public EditStudentFormViewModel(
            NrcManager nrcManager,
            Building building,
            Floor floor,
            Room room,
            int studentId,
            Func<Task>? navigateBackCallback = null)
        {
            StudentId = studentId;
            _navigateBackCallback = navigateBackCallback;

            StudentNrc = new NrcInputViewModel(nrcManager);
            FatherNrc = new NrcInputViewModel(nrcManager);
            MotherNrc = new NrcInputViewModel(nrcManager);
            StudentDateOfBirth = new BirthdayInputViewModel();
            StudentMajor = new MajorInputViewModel();

            // Initial assignments
            SelectedBuilding = building;
            SelectedFloor = floor;
            Room = room;

            _ = InitializeFormAsync();
        }

        public EditStudentFormViewModel(
            Building building,
            Floor floor,
            Room room,
            int studentId,
            Func<Task>? navigateBackCallback = null)
            : this(NrcManager.Instance, building, floor, room, studentId, navigateBackCallback) { }

        private async Task InitializeFormAsync()
        {
            await LoadBuildingsAsync();
            await LoadStudentDetailsAsync();
        }

        // Cascading Dropdown Handlers
        partial void OnSelectedBuildingChanged(Building? value)
        {
            _ = LoadFloorsForBuildingAsync(value);
        }

        partial void OnSelectedFloorChanged(Floor? value)
        {
            _ = LoadRoomsForFloorAsync(value);
        }

        private async Task LoadBuildingsAsync()
        {
            await using var db = new AppDbContext();
            var buildings = await db.Buildings.AsNoTracking().ToListAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                AvailableBuildings.Clear();
                foreach (var b in buildings) AvailableBuildings.Add(b);
            });
        }

        private async Task LoadFloorsForBuildingAsync(Building? building)
        {
            AvailableFloors.Clear();
            AvailableRooms.Clear();

            if (building == null) return;

            await using var db = new AppDbContext();
            var floors = await db.Floors
                .Where(f => f.BuildingId == building.Id)
                .AsNoTracking()
                .ToListAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var f in floors) AvailableFloors.Add(f);
            });
        }

        private async Task LoadRoomsForFloorAsync(Floor? floor)
        {
            AvailableRooms.Clear();

            if (floor == null) return;

            await using var db = new AppDbContext();
            var rooms = await db.Rooms
                .Where(r => r.FloorId == floor.Id)
                .AsNoTracking()
                .ToListAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var r in rooms) AvailableRooms.Add(r);
            });
        }

        // ==========================================
        // Load Existing Student Data
        // ==========================================
        private async Task LoadStudentDetailsAsync()
        {
            try
            {
                IsLoading = true;
                Student? student;

                await using (var db = new AppDbContext())
                {
                    student = await db.Students
                        .AsNoTracking()
                        .Include(s => s.AcademicRecords)
                        .Include(s => s.Room)
                            .ThenInclude(r => r.Floor)
                                .ThenInclude(f => f.Building)
                        .FirstOrDefaultAsync(s => s.Id == StudentId);
                }

                if (student == null)
                {
                    MessageBox.Show("ကျောင်းသား အချက်အလက် ရှာမတွေ့ပါ။", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    await NavigateBackAsync();
                    return;
                }

                StudentName = student.Name ?? string.Empty;
                FatherName = student.Father_name ?? string.Empty;
                MotherName = student.Mother_name ?? string.Empty;
                StudentPh = student.Student_ph ?? string.Empty;
                ParentPh = student.Parent_ph ?? string.Empty;
                Address = student.Address ?? string.Empty;
                SelectedGender = student.Gender ?? string.Empty;

                HighSchoolRollNo = student.High_school_roll_no ?? string.Empty;
                HighSchoolName = student.High_school_name ?? string.Empty;
                HighSchoolCity = student.High_school_city ?? string.Empty;

                StudentDateOfBirth.SetDate(student.Birthday);
                StudentMajor.SelectedMajor = student.Major;
                StudentNrc.SetNrc(student.NRC);
                FatherNrc.SetNrc(student.Father_NRC);
                MotherNrc.SetNrc(student.Mother_NRC);

                ExistingProfilePhotoPath = student.Img ?? string.Empty;
                ExistingHighSchoolCertPath = student.High_school_certificate_img ?? string.Empty;
                ExistingNrcFrontImagePath = student.NRC_img_front ?? string.Empty;
                ExistingNrcBackImagePath = student.NRC_img_back ?? string.Empty;

                _studentPhotoPath = ExistingProfilePhotoPath;
                StudentPhotoPreviewSource = LoadBitmapImageSafely(_studentPhotoPath);

                _highSchoolCertPath = ExistingHighSchoolCertPath;
                HighSchoolCertPreviewSource = LoadBitmapImageSafely(_highSchoolCertPath);

                _nrcFrontImagePath = ExistingNrcFrontImagePath;
                NrcFrontPreviewSource = LoadBitmapImageSafely(_nrcFrontImagePath);

                _nrcBackImagePath = ExistingNrcBackImagePath;
                NrcBackPreviewSource = LoadBitmapImageSafely(_nrcBackImagePath);

                // Re-bind current room selections dynamically
                if (student.Room != null)
                {
                    SelectedBuilding = AvailableBuildings.FirstOrDefault(b => b.Id == student.Room.Floor.BuildingId);
                    await LoadFloorsForBuildingAsync(SelectedBuilding);

                    SelectedFloor = AvailableFloors.FirstOrDefault(f => f.Id == student.Room.FloorId);
                    await LoadRoomsForFloorAsync(SelectedFloor);

                    Room = AvailableRooms.FirstOrDefault(r => r.Id == student.RoomId);
                }

                AcademicRecords.Clear();
                if (student.AcademicRecords != null)
                {
                    foreach (var record in student.AcademicRecords)
                    {
                        AcademicRecords.Add(new AcademicRecordInputViewModel
                        {
                            AcademicYearLevel = record.AcademicYearLevel,
                            AttemptNumber = record.AttemptNumber,
                            RollNumber = record.RollNumber ?? string.Empty,
                            AttendedYear = record.AttendedYear ?? string.Empty,
                            IsHonours = record.IsHonours
                        });
                    }
                }

                OnPropertyChanged(nameof(StudentNrc));
                OnPropertyChanged(nameof(FatherNrc));
                OnPropertyChanged(nameof(MotherNrc));
                OnPropertyChanged(nameof(StudentDateOfBirth));
                OnPropertyChanged(nameof(StudentMajor));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"အချက်အလက်များ ဆွဲယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==========================================
        // Save Updates
        // ==========================================
        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsSaving) return;

            if (Room == null)
            {
                MessageBox.Show("ကျေးဇူးပြု၍ အခန်း ရွေးချယ်ပါ။", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!StudentNrc.Validate("Student") || !FatherNrc.Validate("Father") ||
                !MotherNrc.Validate("Mother") || !StudentDateOfBirth.Validate("Date of Birth") ||
                !StudentMajor.Validate("Major"))
            {
                
                MessageBox.Show("ကျေးဇူးပြု၍ လိုအပ်သော အချက်အလက်များကို မှန်ကန်စွာ ဖြည့်သွင်းပါ။", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsSaving = true;

                string profilePhotoPath = string.Empty;
                string highSchoolCertPath = string.Empty;
                string nrcFrontImagePath = string.Empty;
                string nrcBackImagePath = string.Empty;

                await Task.Run(() =>
                {
                    profilePhotoPath = _imageManager.SaveImage(_studentPhotoPath, ExistingProfilePhotoPath, "StudentPhotos");
                    highSchoolCertPath = _imageManager.SaveImage(_highSchoolCertPath, ExistingHighSchoolCertPath, "Certificates");
                    nrcFrontImagePath = _imageManager.SaveImage(_nrcFrontImagePath, ExistingNrcFrontImagePath, "NrcImages");
                    nrcBackImagePath = _imageManager.SaveImage(_nrcBackImagePath, ExistingNrcBackImagePath, "NrcImages");
                });

                var dto = new UpdateStudentDto
                {
                    Id = StudentId,
                    RoomId = Room.Id,
                    Name = StudentName?.Trim() ?? string.Empty,
                    Gender = SelectedGender ?? string.Empty,
                    Birthday = StudentDateOfBirth.SelectedDate ?? DateTime.MinValue,
                    NRC = StudentNrc.FullNrc,
                    Student_ph = StudentPh?.Trim() ?? string.Empty,
                    Parent_ph = ParentPh?.Trim() ?? string.Empty,
                    Address = Address?.Trim() ?? string.Empty,
                    Father_name = FatherName?.Trim() ?? string.Empty,
                    Mother_name = MotherName?.Trim() ?? string.Empty,
                    Father_NRC = FatherNrc.FullNrc,
                    Mother_NRC = MotherNrc.FullNrc,
                    Major = StudentMajor.SelectedMajor,
                    High_school_roll_no = HighSchoolRollNo?.Trim() ?? string.Empty,
                    High_school_name = HighSchoolName?.Trim() ?? string.Empty,
                    High_school_city = HighSchoolCity?.Trim() ?? string.Empty,
                    Img = profilePhotoPath,
                    High_school_certificate_img = highSchoolCertPath,
                    NRC_img_front = nrcFrontImagePath,
                    NRC_img_back = nrcBackImagePath,
                    AcademicRecords = AcademicRecords.Select(r => new UpdateStudentAcademicRecordDto
                    {
                        AcademicYearLevel = r.AcademicYearLevel,
                        AttemptNumber = r.AttemptNumber,
                        RollNumber = r.RollNumber?.Trim() ?? string.Empty,
                        AttendedYear = r.AttendedYear?.Trim() ?? string.Empty,
                        IsHonours = r.IsHonours
                    }).ToList()
                };

                await using (var db = new AppDbContext())
                {
                    var existingStudent = await db.Students
                        .Include(s => s.AcademicRecords)
                        .FirstOrDefaultAsync(s => s.Id == dto.Id);

                    if (existingStudent != null)
                    {
                        if (existingStudent.Img != dto.Img && !string.IsNullOrWhiteSpace(existingStudent.Img))
                            _imageManager.DeleteImage(existingStudent.Img);

                        if (existingStudent.High_school_certificate_img != dto.High_school_certificate_img && !string.IsNullOrWhiteSpace(existingStudent.High_school_certificate_img))
                            _imageManager.DeleteImage(existingStudent.High_school_certificate_img);

                        if (existingStudent.NRC_img_front != dto.NRC_img_front && !string.IsNullOrWhiteSpace(existingStudent.NRC_img_front))
                            _imageManager.DeleteImage(existingStudent.NRC_img_front);

                        if (existingStudent.NRC_img_back != dto.NRC_img_back && !string.IsNullOrWhiteSpace(existingStudent.NRC_img_back))
                            _imageManager.DeleteImage(existingStudent.NRC_img_back);

                        existingStudent.RoomId = dto.RoomId;
                        existingStudent.Name = dto.Name;
                        existingStudent.Gender = dto.Gender;
                        existingStudent.Birthday = dto.Birthday;
                        existingStudent.NRC = dto.NRC;
                        existingStudent.Student_ph = dto.Student_ph;
                        existingStudent.Parent_ph = dto.Parent_ph;
                        existingStudent.Address = dto.Address;
                        existingStudent.Father_name = dto.Father_name;
                        existingStudent.Mother_name = dto.Mother_name;
                        existingStudent.Father_NRC = dto.Father_NRC;
                        existingStudent.Mother_NRC = dto.Mother_NRC;
                        existingStudent.Major = dto.Major;
                        existingStudent.High_school_roll_no = dto.High_school_roll_no;
                        existingStudent.High_school_name = dto.High_school_name;
                        existingStudent.High_school_city = dto.High_school_city;
                        existingStudent.Img = dto.Img;
                        existingStudent.High_school_certificate_img = dto.High_school_certificate_img;
                        existingStudent.NRC_img_front = dto.NRC_img_front;
                        existingStudent.NRC_img_back = dto.NRC_img_back;

                        existingStudent.AcademicRecords.Clear();
                        foreach (var ar in dto.AcademicRecords)
                        {
                            existingStudent.AcademicRecords.Add(new StudentAcademicRecord
                            {
                                StudentId = dto.Id,
                                AcademicYearLevel = ar.AcademicYearLevel,
                                AttemptNumber = ar.AttemptNumber,
                                RollNumber = ar.RollNumber ?? string.Empty,
                                AttendedYear = ar.AttendedYear ?? string.Empty,
                                IsHonours = ar.IsHonours
                            });
                        }

                        await db.SaveChangesAsync();
                        WeakReferenceMessenger.Default.Send(new DatabaseUpdatedMessage("Student", StudentId, DatabaseOperation.Updated));
                    }
                }

                MessageBox.Show("ကျောင်းသား အချက်အလက်များကို ပြင်ဆင်ပြီးပါပြီ။", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ဒေတာပြင်ဆင်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync() => await NavigateBackAsync();

        private async Task NavigateBackAsync()
        {
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
            }
        }

        // Image Commands
        [RelayCommand]
        private void BrowseStudentPhoto()
        {
            string? path = SelectImageFileDialog("Select Student Photo");
            if (path != null) { _studentPhotoPath = path; StudentPhotoPreviewSource = LoadBitmapImageSafely(path); }
        }
        [RelayCommand] private void ClearStudentPhoto() { _studentPhotoPath = string.Empty; StudentPhotoPreviewSource = null; }

        [RelayCommand]
        private void BrowseHighSchoolCert()
        {
            string? path = SelectImageFileDialog("Select Certificate Image");
            if (path != null) { _highSchoolCertPath = path; HighSchoolCertPreviewSource = LoadBitmapImageSafely(path); }
        }
        [RelayCommand] private void ClearHighSchoolCert() { _highSchoolCertPath = string.Empty; HighSchoolCertPreviewSource = null; }

        [RelayCommand]
        private void BrowseNrcFront()
        {
            string? path = SelectImageFileDialog("Select NRC Front");
            if (path != null) { _nrcFrontImagePath = path; NrcFrontPreviewSource = LoadBitmapImageSafely(path); }
        }
        [RelayCommand] private void ClearNrcFront() { _nrcFrontImagePath = string.Empty; NrcFrontPreviewSource = null; }

        [RelayCommand]
        private void BrowseNrcBack()
        {
            string? path = SelectImageFileDialog("Select NRC Back");
            if (path != null) { _nrcBackImagePath = path; NrcBackPreviewSource = LoadBitmapImageSafely(path); }
        }
        [RelayCommand] private void ClearNrcBack() { _nrcBackImagePath = string.Empty; NrcBackPreviewSource = null; }

        [RelayCommand] private void AddAcademicRecord() => AcademicRecords.Add(new AcademicRecordInputViewModel());
        [RelayCommand] private void RemoveAcademicRecord(AcademicRecordInputViewModel? record) { if (record != null) AcademicRecords.Remove(record); }

        private static string? SelectImageFileDialog(string title)
        {
            var dialog = new OpenFileDialog { Title = title, Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*" };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private static BitmapImage? LoadBitmapImageSafely(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
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
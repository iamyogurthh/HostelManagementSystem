using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.DTOs;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.RoomDetailViewModels;
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
        private readonly ImageManager _imageManager = new ImageManager();

        // Callback for returning to the parent caller (e.g., ResidentViewModel)
        private readonly Func<Task>? _navigateBackCallback;

        // ==========================================
        // Parent Navigation & Room Context Properties
        // ==========================================
        [ObservableProperty]
        private Building? _building;

        [ObservableProperty]
        private Floor? _floor;

        [ObservableProperty]
        private Room? _room;

        // Lock room selection when launched from RoomDetailViewModel
        [ObservableProperty]
        private bool _isRoomSelectionEnabled = true;

        // Dynamic collections for Room Selection Dropdowns (used when changing rooms)
        public ObservableCollection<Building> AvailableBuildings { get; } = new();
        public ObservableCollection<Floor> AvailableFloors { get; } = new();
        public ObservableCollection<Room> AvailableRooms { get; } = new();

        // Cascading Selection Handlers
        private Building? _selectedBuilding;
        public Building? SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (SetProperty(ref _selectedBuilding, value))
                {
                    Building = value;

                    // Clear child selections and dropdown collections
                    SelectedFloor = null;
                    Room = null;
                    AvailableFloors.Clear();
                    AvailableRooms.Clear();

                    if (value != null)
                    {
                        _ = LoadFloorsForBuildingAsync(value);
                    }
                }
            }
        }

        private Floor? _selectedFloor;
        public Floor? SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                if (SetProperty(ref _selectedFloor, value))
                {
                    Floor = value;

                    // Clear room selection and collection
                    Room = null;
                    AvailableRooms.Clear();

                    if (value != null)
                    {
                        _ = LoadRoomsForFloorAsync(value);
                    }
                }
            }
        }

        // ==========================================
        // 1. Sub-ViewModels (NRC, Birthday & Major)
        // ==========================================
        public NrcInputViewModel StudentNrc { get; }
        public NrcInputViewModel FatherNrc { get; }
        public NrcInputViewModel MotherNrc { get; }

        public BirthdayInputViewModel StudentDateOfBirth { get; }
        public MajorInputViewModel StudentMajor { get; }

        // ==========================================
        // 2. Personal Information Properties
        // ==========================================
        [ObservableProperty]
        private string _studentName = string.Empty;

        [ObservableProperty]
        private string _fatherName = string.Empty;

        [ObservableProperty]
        private string _motherName = string.Empty;

        [ObservableProperty]
        private string _student_ph = string.Empty;

        [ObservableProperty]
        private string _parent_ph = string.Empty;

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _selectedGender = string.Empty;

        [ObservableProperty]
        private string _genderErrorMessage = string.Empty;

        // High School Details
        [ObservableProperty]
        private string _highSchoolRollNo = string.Empty;

        [ObservableProperty]
        private string _highSchoolName = string.Empty;

        [ObservableProperty]
        private string _highSchoolCity = string.Empty;

        [ObservableProperty]
        private bool _isSaving;

        // ==========================================
        // Student Category
        // ==========================================

        [ObservableProperty]
        private ObservableCollection<StudentCategory> _availableStudentCategories = new();

        [ObservableProperty]
        private StudentCategory? _selectedStudentCategory;

        [ObservableProperty]
        private string _studentCategoryError = string.Empty;

        // ==========================================
        // 3. Dynamic Academic Records Collection
        // ==========================================
        public ObservableCollection<AcademicRecordInputViewModel> AcademicRecords { get; } = new();

        // Dropdown Items for Academic Records UI
        public List<string> YearLevels { get; } = new()
        {
            "ပထမနှစ်", "ဒုတိယနှစ်", "တတိယနှစ်", "စတုတ္ထနှစ်", "ပဉ္စမနှစ်", "ပထမနှစ်-ဂုဏ်ထူးတန်း","ဒုတိယနှစ်-ဂုဏ်ထူးတန်း","တတိယနှစ်-ဂုဏ်ထူးတန်း", "မဟာတန်း"
        };

        public List<int> AttemptNumbers { get; } = new() { 1, 2, 3 };

        // ==========================================
        // 4. Image Preview Properties & File Paths
        // ==========================================
        [ObservableProperty]
        private ImageSource? _studentPhotoPreviewSource;
        private string _studentPhotoPath = string.Empty;
        public string StudentPhotoPath => _studentPhotoPath;

        [ObservableProperty]
        private ImageSource? _highSchoolCertPreviewSource;
        private string _highSchoolCertPath = string.Empty;
        public string HighSchoolCertPath => _highSchoolCertPath;

        [ObservableProperty]
        private ImageSource? _nrcFrontPreviewSource;
        private string _nrcFrontImagePath = string.Empty;
        public string NrcFrontImagePath => _nrcFrontImagePath;

        [ObservableProperty]
        private ImageSource? _nrcBackPreviewSource;
        private string _nrcBackImagePath = string.Empty;
        public string NrcBackImagePath => _nrcBackImagePath;

        // ==========================================
        // 5. Constructors
        // ==========================================

        // Standalone Constructor (Used from ResidentViewModel / Standalone mode)
        public AddNewStudentFormViewModel(Func<Task>? navigateBackCallback = null)
            : this(NrcManager.Instance, navigateBackCallback) { }

        public AddNewStudentFormViewModel(NrcManager nrcManager, Func<Task>? navigateBackCallback = null)
        {
            _navigateBackCallback = navigateBackCallback;
            IsRoomSelectionEnabled = true;

            StudentNrc = new NrcInputViewModel(nrcManager);
            FatherNrc = new NrcInputViewModel(nrcManager);
            MotherNrc = new NrcInputViewModel(nrcManager);
            StudentDateOfBirth = new BirthdayInputViewModel();
            StudentMajor = new MajorInputViewModel();

            // Load available buildings asynchronously for room selection dropdowns
            _ = LoadBuildingsAsync();
            _ = LoadStudentCategoriesAsync();
        }

        // Room Context Constructors (Used from RoomDetailViewModel)
        public AddNewStudentFormViewModel(NrcManager nrcManager, Building building, Floor floor, Room room, Func<Task>? navigateBackCallback = null)
            : this(nrcManager, navigateBackCallback)
        {
            IsRoomSelectionEnabled = false; // Lock room dropdown so user registers into the pre-selected room

            // Initialize pre-selected hierarchy asynchronously to preserve WPF bindings
            _ = InitializeRoomContextAsync(building, floor, room);
        }

        public AddNewStudentFormViewModel(Building building, Floor floor, Room room, Func<Task>? navigateBackCallback = null)
            : this(NrcManager.Instance, building, floor, room, navigateBackCallback) { }

        // Initialize pre-selected hierarchy when launched from RoomDetailViewModel
        private async Task InitializeRoomContextAsync(Building building, Floor floor, Room room)
        {
            await LoadBuildingsAsync();

            var matchingBuilding = AvailableBuildings.FirstOrDefault(b => b.Id == building.Id);
            if (matchingBuilding != null)
            {
                SelectedBuilding = matchingBuilding;
                await LoadFloorsForBuildingAsync(matchingBuilding);

                var matchingFloor = AvailableFloors.FirstOrDefault(f => f.Id == floor.Id);
                if (matchingFloor != null)
                {
                    SelectedFloor = matchingFloor;
                    await LoadRoomsForFloorAsync(matchingFloor);

                    var matchingRoom = AvailableRooms.FirstOrDefault(r => r.Id == room.Id);
                    if (matchingRoom != null)
                    {
                        Room = matchingRoom;
                    }
                }
            }
        }

        // ==========================================
        // Cascading Room Selection Helpers
        // ==========================================
        private async Task LoadBuildingsAsync()
        {
            await using var db = new AppDbContext();
            var buildings = await db.Buildings.AsNoTracking().ToListAsync();

            AvailableBuildings.Clear();
            foreach (var b in buildings)
            {
                AvailableBuildings.Add(b);
            }
        }

        private async Task LoadFloorsForBuildingAsync(Building? building)
        {
            if (building == null) return;

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

        private async Task LoadRoomsForFloorAsync(Floor? floor)
        {
            if (floor == null) return;

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

        // ==========================================
        // 6. Dynamic Academic Record Commands
        // ==========================================
        [RelayCommand]
        private void AddAcademicRecord()
        {
            AcademicRecords.Add(new AcademicRecordInputViewModel());
        }

        [RelayCommand]
        private void RemoveAcademicRecord(AcademicRecordInputViewModel record)
        {
            if (record != null && AcademicRecords.Contains(record))
            {
                AcademicRecords.Remove(record);
            }
        }

        // ==========================================
        // 7. Image Commands
        // ==========================================
        [RelayCommand]
        private void BrowseStudentPhoto()
        {
            string? filePath = SelectImageFileDialog("Select Student Photo");
            if (!string.IsNullOrEmpty(filePath))
            {
                _studentPhotoPath = filePath;
                StudentPhotoPreviewSource = LoadBitmapImage(filePath);
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
            string? filePath = SelectImageFileDialog("Select High School Certificate Image");
            if (!string.IsNullOrEmpty(filePath))
            {
                _highSchoolCertPath = filePath;
                HighSchoolCertPreviewSource = LoadBitmapImage(filePath);
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
            string? filePath = SelectImageFileDialog("Select NRC Front Image");
            if (!string.IsNullOrEmpty(filePath))
            {
                _nrcFrontImagePath = filePath;
                NrcFrontPreviewSource = LoadBitmapImage(filePath);
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
            string? filePath = SelectImageFileDialog("Select NRC Back Image");
            if (!string.IsNullOrEmpty(filePath))
            {
                _nrcBackImagePath = filePath;
                NrcBackPreviewSource = LoadBitmapImage(filePath);
            }
        }

        [RelayCommand]
        private void ClearNrcBack()
        {
            _nrcBackImagePath = string.Empty;
            NrcBackPreviewSource = null;
        }

        // ==========================================
        // 8. Action Commands (Save / Cancel)
        // ==========================================
        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsSaving) return;

            // Check room selection validation
            if (Room == null)
            {
                MessageBox.Show("ကျေးဇူးပြု၍ အခန်း ရွေးချယ်ပါ။",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // 1. Validate Sub-ViewModels
            bool isStudentNrcValid = StudentNrc.Validate("Student");
            bool isFatherNrcValid = FatherNrc.Validate("Father");
            bool isMotherNrcValid = MotherNrc.Validate("Mother");
            bool isDobValid = StudentDateOfBirth.Validate("Date of Birth");
            bool isMajorValid = StudentMajor.Validate("Major");
            bool isStudentCategoryValid = ValidateStudentCategory();

            if (!isStudentNrcValid || !isFatherNrcValid || !isMotherNrcValid || !isDobValid || !isMajorValid || !isStudentCategoryValid)
            {
                
                MessageBox.Show("ကျေးဇူးပြု၍ လိုအပ်သော အချက်အလက်များကို မှန်ကန်စွာ ဖြည့်သွင်းပါ။",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsSaving = true;

                // 2. Offload image copying to background thread using ImageManager
                string profilePhotoPath = string.Empty;
                string highSchoolCertPath = string.Empty;
                string nrcFrontImagePath = string.Empty;
                string nrcBackImagePath = string.Empty;

                await Task.Run(() =>
                {
                    profilePhotoPath = _imageManager.SaveImage(_studentPhotoPath, string.Empty, "StudentPhotos");
                    highSchoolCertPath = _imageManager.SaveImage(_highSchoolCertPath, string.Empty, "Certificates");
                    nrcFrontImagePath = _imageManager.SaveImage(_nrcFrontImagePath, string.Empty, "NrcImages");
                    nrcBackImagePath = _imageManager.SaveImage(_nrcBackImagePath, string.Empty, "NrcImages");
                });

                // 3. Build DTO from UI fields
                var studentDto = new CreateStudentDto
                {
                    RoomId = Room.Id,

                    StudentCategoryId = SelectedStudentCategory!.Id,
                    Name = StudentName.Trim(),
                    Gender = SelectedGender,
                    Birthday = StudentDateOfBirth.SelectedDate ?? DateTime.MinValue,
                    NRC = StudentNrc.FullNrc,
                    Student_ph = Student_ph.Trim(),
                    Parent_ph = Parent_ph.Trim(),
                    Address = Address.Trim(),

                    Father_name = FatherName.Trim(),
                    Mother_name = MotherName.Trim(),
                    Father_NRC = FatherNrc.FullNrc,
                    Mother_NRC = MotherNrc.FullNrc,

                    Major = StudentMajor.SelectedMajor,

                    High_school_roll_no = HighSchoolRollNo.Trim(),
                    High_school_name = HighSchoolName.Trim(),
                    High_school_city = HighSchoolCity.Trim(),

                    Img = profilePhotoPath,
                    High_school_certificate_img = highSchoolCertPath,
                    NRC_img_front = nrcFrontImagePath,
                    NRC_img_back = nrcBackImagePath,

                    AcademicRecords = AcademicRecords.Select(r => new CreateStudentAcademicRecordDto
                    {
                        AcademicYearLevel = r.AcademicYearLevel,
                        AttemptNumber = r.AttemptNumber,
                        RollNumber = r.RollNumber?.Trim() ?? string.Empty,
                        AttendedYear = r.AttendedYear?.Trim() ?? string.Empty,
                        IsHonours = r.IsHonours
                    }).ToList()
                };

                // 4. Map DTO to Domain Model Entity
                var newStudent = MapDtoToEntity(studentDto);

                // 5. Save asynchronously into AppDbContext
                await using (var db = new AppDbContext())
                {
                    await db.Students.AddAsync(newStudent);
                    await db.SaveChangesAsync();
                }

                //Update the Dashboard
                WeakReferenceMessenger.Default.Send(new DatabaseUpdatedMessage("Student", newStudent.Id, DatabaseOperation.Inserted));

                //Show success message
                MessageBox.Show("ကျောင်းသား အချက်အလက်များကို အောင်မြင်စွာ သိမ်းဆည်းပြီးပါပြီ။",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);


                // 6. Navigate back
                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ဒေတာသိမ်းဆည်းရာတွင် အမှားယွင်းဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await NavigateBackAsync();
        }

        // RelayCommand မလို class ထဲမှာဘဲ ခေါ်သုံးထား
        private async Task NavigateBackAsync()
        {
            // If explicit back callback was passed (e.g. from ResidentViewModel), run it
            if (_navigateBackCallback != null)
            {
                await _navigateBackCallback.Invoke();
                return;
            }

            // Fallback: return to RoomDetailViewModel if room context exists
            if (Building != null && Floor != null && Room != null)
            {
                var roomDetailVm = new RoomDetailViewModel(Building, Floor, Room);
                WeakReferenceMessenger.Default.Send(new NavigateMessage(roomDetailVm));
                await roomDetailVm.LoadRoomDetailByRoomIdCommand.ExecuteAsync(null);
            }
        }

        [RelayCommand]
        private async Task ExportFormTemplateAsync()
        {
            try
            {
                ExportPDFService exportPDFService = new ExportPDFService();
                await exportPDFService.ExportFormTemplateAsPdfAsync();
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Unable to save the PDF. Please close the file which is open in another program and try again.\n\nError: {ex.Message}",
                    "File Access Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await Task.CompletedTask;
        }

        private async Task LoadStudentCategoriesAsync()
        {
            try
            {
                await using var db = new AppDbContext();

                var categories = await db.StudentCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                AvailableStudentCategories.Clear();

                foreach (var category in categories)
                {
                    AvailableStudentCategories.Add(category);
                }
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
        // ==========================================
        // Mapping Helper
        // ==========================================
        private static Student MapDtoToEntity(CreateStudentDto dto)
        {
            return new Student
            {
                RoomId = dto.RoomId,

                StudentCategoryId = dto.StudentCategoryId,
                Name = dto.Name,
                Gender = dto.Gender,
                Birthday = dto.Birthday,
                NRC = dto.NRC,
                Student_ph = dto.Student_ph,
                Parent_ph = dto.Parent_ph,
                Address = dto.Address,

                Father_name = dto.Father_name,
                Mother_name = dto.Mother_name,
                Father_NRC = dto.Father_NRC,
                Mother_NRC = dto.Mother_NRC,

                Major = dto.Major,

                High_school_roll_no = dto.High_school_roll_no,
                High_school_name = dto.High_school_name,
                High_school_city = dto.High_school_city,

                Img = dto.Img,
                High_school_certificate_img = dto.High_school_certificate_img,
                NRC_img_front = dto.NRC_img_front,
                NRC_img_back = dto.NRC_img_back,

                IsActive = true,
                CreatedAt = DateTime.Now,

                AcademicRecords = dto.AcademicRecords.Select(ar => new StudentAcademicRecord
                {
                    AcademicYearLevel = ar.AcademicYearLevel,
                    AttemptNumber = ar.AttemptNumber,
                    RollNumber = ar.RollNumber,
                    AttendedYear = ar.AttendedYear,
                    IsHonours = ar.IsHonours
                }).ToList()
            };
        }

        // ==========================================
        // Helper Methods
        // ==========================================
        private void ResetForm()
        {
            StudentName = string.Empty;
            FatherName = string.Empty;
            MotherName = string.Empty;
            Student_ph = string.Empty;
            Parent_ph = string.Empty;
            Address = string.Empty;
            SelectedGender = string.Empty;
            SelectedStudentCategory = null;

            HighSchoolRollNo = string.Empty;
            HighSchoolName = string.Empty;
            HighSchoolCity = string.Empty;

            AcademicRecords.Clear();

            ClearStudentPhoto();
            ClearHighSchoolCert();
            ClearNrcFront();
            ClearNrcBack();
        }

        private static string? SelectImageFileDialog(string title)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        private static BitmapImage LoadBitmapImage(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.RelativeOrAbsolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file lock on disk
            bitmap.EndInit();
            bitmap.Freeze(); // Multi-thread UI safety
            return bitmap;
        }

        private bool ValidateStudentCategory()
        {
            if (SelectedStudentCategory == null)
            {
                StudentCategoryError =
                    "ကျောင်းသား/သူ အမျိုးအစား ရွေးချယ်ပါ။";

                return false;
            }

            StudentCategoryError = string.Empty;

            return true;
        }
    }
}
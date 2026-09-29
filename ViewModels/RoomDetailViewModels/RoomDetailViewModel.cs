using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.DTOs;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
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

namespace HostelManagementSystem.ViewModels.RoomDetailViewModels
{
    public partial class RoomDetailViewModel : ObservableObject
    {
        private readonly ImageManager _imageManager = new ImageManager();

        [ObservableProperty]
        private Room _room;

        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        [ObservableProperty]
        private ObservableCollection<StudentDataGridDto> _students = new();

        public RoomDetailViewModel(Building building, Floor floor, Room room)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));
            Room = room ?? throw new ArgumentNullException(nameof(room));

            _ = LoadRoomDetailByRoomIdCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        public async Task LoadRoomDetailByRoomIdAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                List<StudentDataGridDto> studentList;

                await using (var db = new AppDbContext())
                {
                    // Direct projection: Only fetch the required columns from SQLite
                    studentList = await db.Students
                        .AsNoTracking()
                        .Where(s => s.RoomId == Room.Id && s.IsActive)
                        .OrderBy(s => s.Name)
                        .Select(s => new StudentDataGridDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            NRC = s.NRC,
                            Student_ph = s.Student_ph,
                            Father_name = s.Father_name,
                            LatestRollNumber = s.AcademicRecords
                                .OrderByDescending(ar => ar.Id)
                                .Select(ar => ar.RollNumber)
                                .FirstOrDefault() ?? string.Empty
                        })
                        .ToListAsync();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Students = new ObservableCollection<StudentDataGridDto>(studentList);
                });

            }, "အခန်းအချက်အလက်များ ရယူနေပါသည်...");
        }

        [RelayCommand]
        private void NavigateToStudentDetail(int studentId)
        {
            if (studentId <= 0) return;

            // Pass context (Building, Floor, Room, studentId) so StudentDetailViewModel can load data & handle Back navigation
            var detailViewModel = new StudentDetailViewModel(Building, Floor, Room, studentId);
            WeakReferenceMessenger.Default.Send(new NavigateMessage(detailViewModel));
        }

        [RelayCommand]
        private void EditStudent(int studentId)
        {
            if (studentId <= 0) return;

            var editViewModel = new EditStudentFormViewModel(
                Building, Floor, Room, studentId,
                navigateBackCallback: async () =>
                {
                    var roomDetailVm = new RoomDetailViewModel(Building, Floor, Room);
                    WeakReferenceMessenger.Default.Send(new NavigateMessage(roomDetailVm));
                    if (roomDetailVm.LoadRoomDetailByRoomIdCommand.CanExecute(null))
                    {
                        await roomDetailVm.LoadRoomDetailByRoomIdCommand.ExecuteAsync(null);
                    }
                });

            WeakReferenceMessenger.Default.Send(new NavigateMessage(editViewModel));
        }

        [RelayCommand]
        private async Task DeleteStudentAsync(int studentId)
        {
            if (studentId <= 0) return;

            // Find student in local collection for display name in confirmation
            var studentDto = Students.FirstOrDefault(s => s.Id == studentId);
            string studentName = studentDto?.Name ?? "ဤကျောင်းသား";

            // 1. Confirm Deletion
            var result = MessageBox.Show(
                $"ကျောင်းသား '{studentName}' ၏ အချက်အလက်များကို ဖျက်ရန် သေချာပါသလား။",
                "ကျောင်းသား အချက်အလက် ဖျက်ရန် အတည်ပြုခြင်း",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    await using (var db = new AppDbContext())
                    {
                        // 2. Fetch full student record including related academic records
                        var student = await db.Students
                            .Include(s => s.AcademicRecords)
                            .FirstOrDefaultAsync(s => s.Id == studentId);

                        if (student == null)
                        {
                            MessageBox.Show("ကျောင်းသား အချက်အလက် ရှာမတွေ့ပါ။", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 3. Clean up physical image files on background thread
                        await Task.Run(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(student.Img))
                                _imageManager.DeleteImage(student.Img);

                            if (!string.IsNullOrWhiteSpace(student.High_school_certificate_img))
                                _imageManager.DeleteImage(student.High_school_certificate_img);

                            if (!string.IsNullOrWhiteSpace(student.NRC_img_front))
                                _imageManager.DeleteImage(student.NRC_img_front);

                            if (!string.IsNullOrWhiteSpace(student.NRC_img_back))
                                _imageManager.DeleteImage(student.NRC_img_back);
                        });

                        // 4. Delete related academic records & student record from DB
                        if (student.AcademicRecords != null && student.AcademicRecords.Any())
                        {
                            db.StudentAcademicRecords.RemoveRange(student.AcademicRecords);
                        }

                        db.Students.Remove(student);
                        await db.SaveChangesAsync();
                    }

                    // 5. Remove item from UI collection
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var target = Students.FirstOrDefault(s => s.Id == studentId);
                        if (target != null)
                        {
                            Students.Remove(target);
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ဖျက်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့သည်: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, "ကျောင်းသား အချက်အလက် ဖျက်နေပါသည်...");
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            var roomViewModel = new RoomViewModel(Building, Floor);
            WeakReferenceMessenger.Default.Send(new NavigateMessage(roomViewModel));
            await roomViewModel.LoadRoomsByFloorIdCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void OpenAddNewStudentForm()
        {
            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(new AddNewStudentFormViewModel(Building, Floor, Room)));
        }
    }
}
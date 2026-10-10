using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.DTOs;
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

namespace HostelManagementSystem.ViewModels.RoomDetailViewModels
{
    public partial class RoomDetailViewModel : ObservableObject
    {
        // ============================================================
        // ROOM CONTEXT
        // ============================================================

        [ObservableProperty]
        private Room _room;

        [ObservableProperty]
        private Building _building;

        [ObservableProperty]
        private Floor _floor;

        // ============================================================
        // STUDENTS
        // ============================================================

        [ObservableProperty]
        private ObservableCollection<StudentDataGridDto> _students = new();

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public RoomDetailViewModel(
            Building building,
            Floor floor,
            Room room)
        {
            Building =
                building ??
                throw new ArgumentNullException(
                    nameof(building));

            Floor =
                floor ??
                throw new ArgumentNullException(
                    nameof(floor));

            Room =
                room ??
                throw new ArgumentNullException(
                    nameof(room));

            _ = LoadRoomDetailByRoomIdCommand
                .ExecuteAsync(null);
        }

        // ============================================================
        // LOAD ROOM DETAILS
        // ============================================================

        [RelayCommand]
        public async Task LoadRoomDetailByRoomIdAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(
                async () =>
                {
                    await using var db =
                        new AppDbContext();

                    List<StudentDataGridDto> studentList =
                        await db.StudentAccommodations
                            .AsNoTracking()
                            .Where(a =>
                                a.RoomId == Room.Id &&
                                a.IsActive &&
                                a.EndDate == null &&
                                a.Student.IsActive)
                            .OrderBy(a => a.Student.Name)
                            .Select(a =>
                                new StudentDataGridDto
                                {
                                    Id =
                                        a.Student.Id,

                                    Name =
                                        a.Student.Name,

                                    NRC =
                                        a.Student.NRC,

                                    Student_ph =
                                        a.Student.Student_ph,

                                    Father_name =
                                        a.Student.Father_name,

                                    LatestRollNumber =
                                        a.Student.AcademicRecords
                                            .OrderByDescending(
                                                ar => ar.Id)
                                            .Select(
                                                ar =>
                                                    ar.RollNumber)
                                            .FirstOrDefault()
                                        ?? string.Empty
                                })
                            .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Students =
                            new ObservableCollection<StudentDataGridDto>(
                                studentList);
                    });
                },
                "အခန်းအချက်အလက်များ ရယူနေပါသည်...");
        }

        // ============================================================
        // STUDENT DETAIL
        // ============================================================

        [RelayCommand]
        private void NavigateToStudentDetail(
            int studentId)
        {
            if (studentId <= 0)
                return;

            var detailViewModel =
                new StudentDetailViewModel(
                    studentId,
                    navigateBackCallback: async () =>
                    {
                        var roomDetailViewModel =
                            new RoomDetailViewModel(
                                Building,
                                Floor,
                                Room);

                        WeakReferenceMessenger.Default.Send(
                            new NavigateMessage(
                                roomDetailViewModel));

                        await Task.CompletedTask;
                    });

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    detailViewModel));
        }

        // ============================================================
        // EDIT STUDENT
        // ============================================================

        [RelayCommand]
        private void EditStudent(
            int studentId)
        {
            if (studentId <= 0)
                return;

            var editViewModel =
                new EditStudentFormViewModel(
                    studentId,
                    navigateBackCallback: async () =>
                    {
                        var roomDetailViewModel =
                            new RoomDetailViewModel(
                                Building,
                                Floor,
                                Room);

                        WeakReferenceMessenger.Default.Send(
                            new NavigateMessage(
                                roomDetailViewModel));

                        await Task.CompletedTask;
                    });

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    editViewModel));
        }

        // ============================================================
        // DELETE STUDENT
        // ============================================================

        [RelayCommand]
        private async Task DeleteStudentAsync(
            int studentId)
        {
            if (studentId <= 0)
                return;

            var studentDto =
                Students.FirstOrDefault(
                    s => s.Id == studentId);

            string studentName =
                studentDto?.Name
                ?? "ဤကျောင်းသား";

            var result =
                MessageBox.Show(
                    $"ကျောင်းသား '{studentName}' ၏ အချက်အလက်များကို ဖျက်ရန် သေချာပါသလား။",
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

                        // =================================================
                        // SOFT DELETE
                        // =================================================
                        //
                        // This should:
                        // 1. Close the current accommodation.
                        // 2. Set Student.IsActive = false.
                        // 3. Preserve accommodation history.
                        // 4. Preserve financial records.
                        // 5. Preserve student images.
                        //
                        await studentService
                            .DeleteStudentWithRelatedDataAsync(
                                studentId,
                                DateOnly.FromDateTime(
                                    DateTime.Today));

                        // Reload the room.
                        // The deleted student will disappear because
                        // the query only includes active students.
                        await LoadRoomDetailByRoomIdAsync();

                        WeakReferenceMessenger.Default.Send(
                            new DatabaseUpdatedMessage(
                                "Student",
                                studentId,
                                DatabaseOperation.Deleted));
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
                "ကျောင်းသား အချက်အလက်များ ဖျက်နေပါသည်...");
        }

        // ============================================================
        // BACK TO ROOM LIST
        // ============================================================

        [RelayCommand]
        private async Task BackAsync()
        {
            var roomViewModel =
                new RoomViewModel(
                    Building,
                    Floor);

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    roomViewModel));

            await roomViewModel
                .LoadRoomsByFloorIdCommand
                .ExecuteAsync(null);
        }
    }
}


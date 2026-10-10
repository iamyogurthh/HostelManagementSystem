using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Utilities;
using HostelManagementSystem.ViewModels.RoomViewModels.RoomTypeViewModels;
using HostelManagementSystem.ViewModels.StudentCategoryViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _totalStudents;

    [ObservableProperty]
    private int _totalRooms;

    [ObservableProperty]
    private int _availableRooms;

    [ObservableProperty]
    private double _occupancyRate;

    [ObservableProperty]
    private double _totalCapacity;

    [ObservableProperty]
    private ObservableCollection<BuildingOccupancyItem> _buildingOccupancies = new();

    [ObservableProperty]
    private ObservableCollection<MajorDistributionItem> _topMajors = new();

    [ObservableProperty]
    private GenderDistributionData _genderData = new(0, 0);

    [ObservableProperty]
    private ObservableCollection<YearLevelItem> _yearLevelDistributions = new();

    private static readonly List<string> DefinedYearLevels = new()
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

    public DashboardViewModel()
    {
        WeakReferenceMessenger.Default.Register<DatabaseUpdatedMessage>(
            this,
            async (recipient, message) =>
            {
                if (message.EntityName is "Student"
                    or "Room"
                    or "Building"
                    or "Floor")
                {
                    await LoadDashboardAsync();
                }
            });

        _ = InitialLoadAsync();
    }

    private async Task InitialLoadAsync()
    {
        try
        {
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[Dashboard InitialLoad Error]: {ex.Message}");
        }
    }

    // ============================================================
    // QUICK ACTIONS
    // ============================================================

    [RelayCommand]
    private void OpenStudentCategoryManagement()
    {
        var viewModel = new StudentCategoryViewModel(
            async () =>
            {
                WeakReferenceMessenger.Default.Send(
                    new NavigateMessage(this));

                await Task.CompletedTask;
            });

        WeakReferenceMessenger.Default.Send(
            new NavigateMessage(viewModel));

        _ = viewModel.LoadCategoriesCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void OpenRoomTypeManagement()
    {
        var viewModel = new RoomTypeViewModel(
            async () =>
            {
                WeakReferenceMessenger.Default.Send(
                    new NavigateMessage(this));

                await Task.CompletedTask;
            });

        WeakReferenceMessenger.Default.Send(
            new NavigateMessage(viewModel));

        _ = viewModel.LoadRoomTypesCommand.ExecuteAsync(null);
    }

    // ============================================================
    // DASHBOARD
    // ============================================================

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        await LoadingHelper.ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                await using var db = new AppDbContext();

                // ====================================================
                // CORE KPIs
                // ====================================================

                TotalStudents = await db.Students
                    .CountAsync(s => s.IsActive);

                TotalRooms = await db.Rooms
                    .CountAsync(r => r.IsActive);

                AvailableRooms = await db.Rooms
                    .Where(r =>
                        r.IsActive &&
                        r.Accommodations.Count(a =>
                            a.IsActive &&
                            a.EndDate == null) < r.Capacity)
                    .CountAsync();

                TotalCapacity = await db.Rooms
                    .Where(r => r.IsActive)
                    .SumAsync(r => r.Capacity);

                OccupancyRate = TotalCapacity > 0
                    ? Math.Round(
                        ((double)TotalStudents / TotalCapacity) * 100,
                        1)
                    : 0;

                // ====================================================
                // BUILDING OCCUPANCY
                // ====================================================

                var buildings = await db.Rooms
                    .Where(r =>
                        r.IsActive &&
                        r.Floor.IsActive &&
                        r.Floor.Building.IsActive)
                    .GroupBy(r => r.Floor.Building.Name)
                    .Select(g => new BuildingOccupancyItem(
                        g.Key,
                        g.Sum(r =>
                            r.Accommodations.Count(a =>
                                a.IsActive &&
                                a.EndDate == null)),
                        g.Sum(r => r.Capacity)
                    ))
                    .ToListAsync();

                BuildingOccupancies =
                    new ObservableCollection<BuildingOccupancyItem>(
                        buildings);

                // ====================================================
                // STUDENT DISTRIBUTION BY MAJOR
                // ====================================================

                var majorData = await db.Students
                    .Where(s =>
                        s.IsActive &&
                        !string.IsNullOrWhiteSpace(s.Major))
                    .GroupBy(s => s.Major)
                    .Select(g => new
                    {
                        RawMajorName = g.Key,
                        StudentCount = g.Count()
                    })
                    .OrderByDescending(m => m.StudentCount)
                    .ToListAsync();

                TopMajors =
                    new ObservableCollection<MajorDistributionItem>(
                        majorData.Select(m =>
                            new MajorDistributionItem(
                                m.RawMajorName!,
                                m.StudentCount))
                    );

                // ====================================================
                // GENDER DISTRIBUTION
                // ====================================================

                var maleCount = await db.Students
                    .CountAsync(s =>
                        s.IsActive &&
                        s.Gender == "ကျား");

                var femaleCount = await db.Students
                    .CountAsync(s =>
                        s.IsActive &&
                        s.Gender == "မ");

                GenderData =
                    new GenderDistributionData(
                        maleCount,
                        femaleCount);

                // ====================================================
                // YEAR LEVEL DISTRIBUTION
                // ====================================================

                var activeStudentsLatestRecord =
                    await db.Students
                        .Where(s => s.IsActive)
                        .Select(s => s.AcademicRecords
                            .OrderByDescending(r => r.Id)
                            .Select(r => r.AcademicYearLevel)
                            .FirstOrDefault())
                        .Where(year =>
                            !string.IsNullOrWhiteSpace(year))
                        .ToListAsync();

                var yearCounts = activeStudentsLatestRecord
                    .GroupBy(year => year!)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count());

                var yearLevelsList =
                    DefinedYearLevels
                        .Select(year =>
                            new YearLevelItem(
                                year,
                                yearCounts.TryGetValue(
                                    year,
                                    out var count)
                                    ? count
                                    : 0))
                        .ToList();

                YearLevelDistributions =
                    new ObservableCollection<YearLevelItem>(
                        yearLevelsList);
            }
            catch (DbUpdateException dbEx)
            {
                Debug.WriteLine(
                    $"[Dashboard DB Error]: " +
                    $"{dbEx.InnerException?.Message ?? dbEx.Message}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Database error while loading dashboard:\n" +
                        $"{dbEx.InnerException?.Message ?? dbEx.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Dashboard Execution Error]: " +
                    $"{ex.GetType().Name} - {ex.Message}");

                Debug.WriteLine(
                    $"Stack Trace:\n{ex.StackTrace}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Failed to load dashboard data:\n" +
                        $"{ex.Message}\n\n" +
                        "Check Output window for details.",
                        "Dashboard Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }
        });
    }
}

// ================================================================
// DASHBOARD MODEL RECORDS
// ================================================================

public record BuildingOccupancyItem(
    string BuildingName,
    int OccupiedBeds,
    int TotalCapacity)
{
    public double OccupancyPercentage =>
        TotalCapacity > 0
            ? Math.Round(
                ((double)OccupiedBeds / TotalCapacity) * 100,
                1)
            : 0;

    public string ProgressBarColor =>
        OccupancyPercentage >= 100
            ? "#EF4444"
            : "#4F46E5";
}

public record MajorDistributionItem(
    string RawMajorName,
    int StudentCount)
{
    public string MajorName =>
        string.IsNullOrWhiteSpace(RawMajorName)
            ? string.Empty
            : RawMajorName.Split('(')[0].Trim();
}

public record GenderDistributionData(
    int MaleCount,
    int FemaleCount)
{
    public int TotalStudents =>
        MaleCount + FemaleCount;

    public double MalePercentage =>
        TotalStudents > 0
            ? Math.Round(
                ((double)MaleCount / TotalStudents) * 100,
                1)
            : 0;

    public double FemalePercentage =>
        TotalStudents > 0
            ? Math.Round(
                ((double)FemaleCount / TotalStudents) * 100,
                1)
            : 0;
}

public record YearLevelItem(
    string YearLevelName,
    int StudentCount);
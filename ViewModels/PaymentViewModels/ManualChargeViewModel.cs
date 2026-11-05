using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace HostelManagementSystem.ViewModels.PaymentViewModels;

public partial class ManualChargeViewModel : ObservableObject
{
    private const int StudentSearchLimit = 50;

    private readonly PaymentViewModel _previousViewModel;
    private readonly FeeChargeService _feeChargeService = new();
    private CancellationTokenSource? _studentSearchDebounceCts;

    [ObservableProperty]
    private ObservableCollection<Student> _students = new();

    [ObservableProperty]
    private string _studentSearchText = string.Empty;

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private ObservableCollection<FeeRule> _feeRules = new();

    [ObservableProperty]
    private FeeRule? _selectedFeeRule;

    [ObservableProperty]
    private DateTime _periodStart = DateTime.Today;

    [ObservableProperty]
    private DateTime _periodEnd = DateTime.Today;

    [ObservableProperty]
    private DateTime _dueDate = DateTime.Today;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ManualChargeViewModel(PaymentViewModel previousViewModel)
    {
        _previousViewModel = previousViewModel;
        _ = InitializeAsync();
    }

    partial void OnStudentSearchTextChanged(string value)
    {
        ScheduleStudentSearch();
    }

    private async void ScheduleStudentSearch()
    {
        _studentSearchDebounceCts?.Cancel();
        _studentSearchDebounceCts?.Dispose();
        _studentSearchDebounceCts = new CancellationTokenSource();
        var token = _studentSearchDebounceCts.Token;

        try
        {
            await Task.Delay(350, token);
            await SearchStudentsAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnSelectedFeeRuleChanged(FeeRule? value)
    {
        if (value != null)
        {
            Amount = value.MonthlyAmount;
        }
    }

    private async Task InitializeAsync()
    {
        await LoadFeeRulesAsync();
        await SearchStudentsAsync();
    }

    [RelayCommand]
    private async Task SearchStudentsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            await using var db = new AppDbContext();
            var query = db.Students
                .AsNoTracking()
                .Include(student => student.StudentCategory)
                .Where(student => student.IsActive);

            var search = StudentSearchText.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(student =>
                    EF.Functions.Like(student.Name, $"%{search}%") ||
                    EF.Functions.Like(student.NRC, $"%{search}%"));
            }

            var students = await query
                .OrderBy(student => student.Name)
                .Take(StudentSearchLimit)
                .ToListAsync();

            Students = new ObservableCollection<Student>(students);

            if (SelectedStudent != null &&
                Students.All(student => student.Id != SelectedStudent.Id))
            {
                SelectedStudent = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ကျောင်းသားစာရင်း ရှာဖွေရာတွင် အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFeeRulesAsync()
    {
        try
        {
            await using var db = new AppDbContext();

            var rules = await db.FeeRules
                .AsNoTracking()
                .Include(rule => rule.StudentCategory)
                .Include(rule => rule.RoomType)
                .Where(rule => rule.IsActive)
                .OrderBy(rule => rule.StudentCategory!.Name)
                .ThenBy(rule => rule.RoomType!.Name)
                .ThenByDescending(rule => rule.EffectiveFrom)
                .ToListAsync();

            FeeRules = new ObservableCollection<FeeRule>(rules);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"အခကြေးငွေစည်းမျဉ်းများ ရယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedStudent == null)
        {
            ErrorMessage = "ကျေးဇူးပြု၍ ကျောင်းသားကို ရွေးချယ်ပါ။";
            return;
        }

        if (PeriodStart == default || PeriodEnd == default || PeriodEnd.Date < PeriodStart.Date)
        {
            ErrorMessage = "အခကြေးငွေကာလ ရက်စွဲများ မမှန်ကန်ပါ။";
            return;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "အခကြေးငွေပမာဏသည် ၀ ထက်ကြီးရပါမည်။";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "အခကြေးငွေအကြောင်းအရာကို ထည့်သွင်းပါ။";
            return;
        }

        try
        {
            IsLoading = true;

            await _feeChargeService.CreateManualChargeAsync(
                SelectedStudent.Id,
                DateOnly.FromDateTime(PeriodStart.Date),
                DateOnly.FromDateTime(PeriodEnd.Date),
                Amount,
                DateOnly.FromDateTime(DueDate.Date),
                Description.Trim());

            await ReturnToPaymentAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await ReturnToPaymentAsync();
    }

    private async Task ReturnToPaymentAsync()
    {
        await _previousViewModel.LoadStudentsCommand.ExecuteAsync(null);

        if (_previousViewModel.SelectedStudent != null)
        {
            await _previousViewModel.RefreshStudentFinancialDataCommand
                .ExecuteAsync(null);
        }

        WeakReferenceMessenger.Default.Send(
            new NavigateMessage(_previousViewModel));
    }
}

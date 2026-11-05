using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using HostelManagementSystem.Services.Payment;
using HostelManagementSystem.ViewModels.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace HostelManagementSystem.ViewModels.PaymentViewModels
{
    public partial class PaymentViewModel : PaginationViewModel
    {
        // =========================================================
        // SEARCH
        // =========================================================

        [ObservableProperty]
        private string _searchName = string.Empty;

        private CancellationTokenSource? _searchDebounceCts;


        // =========================================================
        // STUDENTS
        // =========================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStudents))]
        [NotifyPropertyChangedFor(nameof(HasNoStudents))]
        private ObservableCollection<Student> _students = new();

        public bool HasStudents =>
            Students.Count > 0;

        public bool HasNoStudents =>
            !HasStudents;


        // =========================================================
        // UNPAID STUDENTS
        // =========================================================

        private HashSet<int> _unpaidStudentIdSet = new();

        private void UpdateUnpaidStudentStatus(
            IEnumerable<Student> students,
            HashSet<int> unpaidStudentIds)
        {
            foreach (var student in students)
            {
                student.IsUnpaid =
                    unpaidStudentIds.Contains(student.Id);
            }
        }

        partial void OnSearchNameChanged(string value)
        {
            ScheduleSearch();
        }

        private async void ScheduleSearch()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            try
            {
                await Task.Delay(350, token);
                CurrentPage = 1;
                await SearchStudentsAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }



        // =========================================================
        // SELECTED STUDENT
        // =========================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedStudent))]
        private Student? _selectedStudent;

        public bool HasSelectedStudent =>
            SelectedStudent != null;


        // =========================================================
        // FINANCIAL DATA
        // =========================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasOutstandingCharges))]
        private ObservableCollection<OutstandingChargeItemViewModel>
            _outstandingCharges = new();

        public bool HasOutstandingCharges =>
            OutstandingCharges.Count > 0;

        [ObservableProperty]
        private ObservableCollection<PaymentItemViewModel> _payments = new();

        [ObservableProperty]
        private decimal _outstandingBalance;


        // =========================================================
        // DASHBOARD METRICS
        // =========================================================

        [ObservableProperty]
        private decimal _totalSystemOutstanding;

        [ObservableProperty]
        private decimal _monthlyCollectedTotal;


        // =========================================================
        // STATE
        // =========================================================

        [ObservableProperty]
        private bool _isLoading;


        // =========================================================
        // SERVICES
        // =========================================================

        private readonly PaymentService _paymentService;


        // =========================================================
        // ASYNC LOAD VERSION
        // =========================================================

        private int _financialLoadVersion;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public PaymentViewModel()
        {
            _paymentService =
                new PaymentService();

            _ = InitializeAsync();
        }


        // =========================================================
        // INITIALIZATION
        // =========================================================

        private async Task InitializeAsync()
        {
            await LoadStudentsAsync();

            await LoadDashboardMetricsAsync();
        }


        // =========================================================
        // LOAD STUDENTS
        // =========================================================

        [RelayCommand]
        public async Task LoadStudentsAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;

                await using var db =
                    new AppDbContext();

                IQueryable<Student> query =
                    db.Students
                        .AsNoTracking()
                        .Where(s => s.IsActive);


                // -------------------------------------------------
                // SEARCH
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(SearchName))
                {
                    var term =
                        SearchName.Trim();

                    query = query.Where(s =>
                        (s.Name != null &&
                         EF.Functions.Like(
                             s.Name,
                             $"%{term}%")) ||

                        (s.NRC != null &&
                         EF.Functions.Like(
                             s.NRC,
                             $"%{term}%")));
                }


                // -------------------------------------------------
                // TOTAL COUNT
                // -------------------------------------------------

                var totalCount =
                    await query.CountAsync();

                UpdatePagination(totalCount);


                // -------------------------------------------------
                // CURRENT PAGE
                // -------------------------------------------------

                var items =
                    await query
                        .OrderByDescending(s =>
                            db.FeeCharges.Any(fc =>
                                fc.StudentId == s.Id &&
                                fc.Status != FeeChargeStatus.Paid))
                        .ThenByDescending(s => s.CreatedAt)
                        
                        .Skip(GetSkipCount())
                        .Take(PageSize)

                        .Include(s =>
                            s.StudentCategory)
                        .Include(s =>
                            s.Accommodations
                                .Where(a =>
                                    a.IsActive &&
                                    a.EndDate == null))

                            .ThenInclude(a =>
                                a.Room)

                                .ThenInclude(r =>
                                    r.Floor)

                                    .ThenInclude(f =>
                                        f.Building)

                        .ToListAsync();


                // -------------------------------------------------
                // LOAD UNPAID STATUS FOR CURRENT PAGE
                // -------------------------------------------------

                var studentIds =
                    items.Select(s => s.Id).ToList();

                var unpaidStudentIds =
                    studentIds.Count == 0
                        ? new List<int>()
                        : await db.FeeCharges
                            .AsNoTracking()
                            .Where(fc =>
                                studentIds.Contains(fc.StudentId) &&
                                fc.Status != FeeChargeStatus.Paid)
                            .Select(fc => fc.StudentId)
                            .Distinct()
                            .ToListAsync();

                _unpaidStudentIdSet =
                    unpaidStudentIds.ToHashSet();


                // -------------------------------------------------
                // SET UI-ONLY UNPAID STATUS
                // -------------------------------------------------

                UpdateUnpaidStudentStatus(
                    items,
                    _unpaidStudentIdSet);


                // -------------------------------------------------
                // UPDATE STUDENT COLLECTION
                // -------------------------------------------------

                Students =
                    new ObservableCollection<Student>(
                        items);


                // -------------------------------------------------
                // KEEP SELECTED STUDENT
                // -------------------------------------------------

                if (SelectedStudent != null)
                {
                    var selectedId =
                        SelectedStudent.Id;

                    var selectedStudent =
                        Students.FirstOrDefault(
                            s => s.Id == selectedId);

                    if (selectedStudent != null)
                    {
                        SelectedStudent =
                            selectedStudent;
                    }
                    else
                    {
                        SelectedStudent = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ကျောင်းသားအချက်အလတ်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }


        // =========================================================
        // SEARCH
        // =========================================================

        [RelayCommand]
        private async Task SearchStudentsAsync()
        {
            CurrentPage = 1;

            _financialLoadVersion++;

            SelectedStudent = null;

            ClearFinancialData();

            await LoadStudentsAsync();
        }


        // =========================================================
        // RESET SEARCH
        // =========================================================

        [RelayCommand]
        private async Task ResetSearchAsync()
        {
            SearchName =
                string.Empty;

            CurrentPage = 1;

            _financialLoadVersion++;

            SelectedStudent = null;

            ClearFinancialData();

            await LoadStudentsAsync();
        }


        // =========================================================
        // SELECT STUDENT
        // =========================================================

        partial void OnSelectedStudentChanged(
            Student? value)
        {
            _financialLoadVersion++;

            var currentVersion =
                _financialLoadVersion;

            if (value == null)
            {
                ClearFinancialData();
                return;
            }

            _ = LoadStudentFinancialDataAsync(
                value.Id,
                currentVersion);
        }


        // =========================================================
        // LOAD STUDENT FINANCIAL DATA
        // =========================================================

        private async Task LoadStudentFinancialDataAsync(
            int studentId,
            int loadVersion)
        {
            try
            {
                IsLoading = true;

                // Refresh unpaid status for current and former residents.
                await RefreshSelectedStudentUnpaidStatusAsync(
                    studentId,
                    loadVersion);

                if (loadVersion != _financialLoadVersion)
                    return;

                // Load outstanding charges, including unpaid charges
                // remaining after the student leaves.
                var outstanding = await _paymentService
                    .GetOutstandingChargesAsync(studentId);

                if (loadVersion != _financialLoadVersion)
                    return;

                OutstandingCharges =
                    new ObservableCollection<OutstandingChargeItemViewModel>(
                        outstanding.Select(charge =>
                            new OutstandingChargeItemViewModel(charge)));

                OutstandingBalance = OutstandingCharges.Sum(
                    item => item.RemainingAmount);

                // Always load payment history, whether or not the
                // student currently has an accommodation.
                var payments = await _paymentService
                    .GetStudentPaymentsAsync(studentId);

                if (loadVersion != _financialLoadVersion)
                    return;

                Payments = new ObservableCollection<PaymentItemViewModel>(
                     payments.Select(p => new PaymentItemViewModel(p)));
            }
            catch (Exception ex)
            {
                if (loadVersion != _financialLoadVersion)
                    return;

                ClearFinancialData();

                MessageBox.Show(
                    $"ငွေပေးချေမှုအချက်အလက်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်: {ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (loadVersion == _financialLoadVersion)
                    IsLoading = false;
            }
        }


        // =========================================================
        // REFRESH SELECTED STUDENT UNPAID STATUS
        // =========================================================

        private async Task RefreshSelectedStudentUnpaidStatusAsync(
            int studentId,
            int loadVersion)
        {
            var hasUnpaidCharge =
                await HasUnpaidChargeAsync(studentId);

            if (loadVersion !=
                _financialLoadVersion)
            {
                return;
            }

            var student =
                Students.FirstOrDefault(
                    s => s.Id == studentId);

            if (student == null)
                return;

            student.IsUnpaid =
                hasUnpaidCharge;
        }


        // =========================================================
        // CHECK UNPAID CHARGE
        // =========================================================

        private async Task<bool> HasUnpaidChargeAsync(
            int studentId)
        {
            await using var db =
                new AppDbContext();

            return await db.FeeCharges
                .AsNoTracking()
                .AnyAsync(fc =>
                    fc.StudentId == studentId &&
                    fc.Status != FeeChargeStatus.Paid);
        }


        // =========================================================
        // GET REMAINING AMOUNT
        // =========================================================

        /// <summary>
        /// Calculates the remaining amount of a fee charge.
        ///
        /// Remaining =
        ///     FeeCharge.Amount
        ///     - Total PaymentAllocation.Amount
        /// </summary>
        public decimal GetRemainingAmount(
            FeeCharge charge)
        {
            if (charge == null)
                return 0m;

            var allocatedAmount =
                charge.Allocations?
                    .Sum(a => a.Amount) ?? 0m;

            return Math.Max(
                0m,
                charge.Amount - allocatedAmount);
        }


        // =========================================================
        // GET PAID AMOUNT
        // =========================================================

        public decimal GetPaidAmount(
            FeeCharge charge)
        {
            if (charge == null)
                return 0m;

            return charge.Allocations?
                       .Sum(a => a.Amount)
                   ?? 0m;
        }


        // =========================================================
        // REFRESH SELECTED STUDENT
        // =========================================================

        [RelayCommand]
        private async Task RefreshStudentFinancialDataAsync()
        {
           

            
                

                if (SelectedStudent == null)
                    return;

                _financialLoadVersion++;

                var currentVersion =
                    _financialLoadVersion;

                await LoadStudentFinancialDataAsync(
                    SelectedStudent.Id,
                    currentVersion);

                await LoadDashboardMetricsAsync();
            
            
        } 


        // =========================================================
        // LOAD DASHBOARD METRICS
        // =========================================================

        private async Task LoadDashboardMetricsAsync()
        {
            try
            {
                var today =
                    DateTime.Today;


                // -------------------------------------------------
                // TOTAL OUTSTANDING
                // -------------------------------------------------

                TotalSystemOutstanding =
                    await _paymentService
                        .GetSystemOutstandingBalanceAsync();


                // -------------------------------------------------
                // CURRENT MONTH COLLECTION
                // -------------------------------------------------

                MonthlyCollectedTotal =
                    await _paymentService
                        .GetMonthlyCollectedTotalAsync(
                            today.Year,
                            today.Month);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ငွေကြေးအကျဉ်းချုပ်အချက်အလက်များ ရယူရာတွင် အမှားတစ်ခု ဖြစ်ပေါက်ခဲ့သည်: {ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // CLEAR FINANCIAL DATA
        // =========================================================

        private void ClearFinancialData()
        {
            OutstandingCharges =
                new ObservableCollection<OutstandingChargeItemViewModel>();

            Payments =
                new ObservableCollection<PaymentItemViewModel>();

            OutstandingBalance = 0m;
        }


        // =========================================================
        // RECORD PAYMENT
        // =========================================================

        [RelayCommand]
        private void OpenRecordPaymentDialog()
        {
            // -----------------------------------------------------
            // STUDENT REQUIRED
            // -----------------------------------------------------

            if (SelectedStudent == null)
            {
                MessageBox.Show(
                    "ကျေးဇူးပြု၍ ငွေပေးချေမည့် ကျောင်းသားကို ရွေးချယ်ပါ။",
                    "ကျောင်းသားရွေးချယ်ရန်",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            // -----------------------------------------------------
            // OUTSTANDING BALANCE REQUIRED
            // -----------------------------------------------------

            if (OutstandingBalance <= 0)
            {
                MessageBox.Show(
                    "ဤကျောင်းသားတွင် ပေးရန်ကျန်ရှိသော အခကြေးငွေ မရှိပါ။",
                    "ငွေပေးချေမှု",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            // -----------------------------------------------------
            // NAVIGATION CALLBACK
            // -----------------------------------------------------

            Func<Task> navigateBackCallback =
            async () =>
            {
                if (SelectedStudent == null)
                    return;

                _financialLoadVersion++;

                var currentVersion =
                    _financialLoadVersion;

                await LoadStudentFinancialDataAsync(
                    SelectedStudent.Id,
                    currentVersion);

                await LoadDashboardMetricsAsync();

                WeakReferenceMessenger.Default.Send(
                    new NavigateMessage(this));
            };


            // -----------------------------------------------------
            // CREATE RECORD PAYMENT VIEWMODEL
            // -----------------------------------------------------

            var recordPaymentVm =
                new RecordPaymentViewModel(
                    SelectedStudent,
                    navigateBackCallback);


            // -----------------------------------------------------
            // NAVIGATE
            // -----------------------------------------------------

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    recordPaymentVm));
        }

        [RelayCommand]
        private void OpenManualCharge()
        {
            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(
                    new ManualChargeViewModel(this)));
        }


        // =========================================================
        // PAGINATION
        // =========================================================

        protected override async Task RefreshPageAsync()
        {
            await LoadStudentsAsync();
        }
    }
}
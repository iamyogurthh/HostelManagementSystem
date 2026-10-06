using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace HostelManagementSystem.ViewModels.FeeRuleViewModels
{
    public partial class FeeRuleViewModel : ObservableObject
    {
        // =========================================================
        // DATA
        // =========================================================

        private List<FeeRule> _allFeeRules = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFeeRules))]
        [NotifyPropertyChangedFor(nameof(HasNoFeeRules))]
        private ObservableCollection<FeeRule> _feeRules = new();

        [ObservableProperty]
        private bool _isLoading;

        // =========================================================
        // FILTERS
        // =========================================================

        [ObservableProperty]
        private DateTime? _filterFromDate;

        [ObservableProperty]
        private DateTime? _filterToDate;

        [ObservableProperty]
        private string _selectedStatus = "အားလုံး";

        public ObservableCollection<string> StatusOptions { get; } =
            new()
            {
                "အားလုံး",
                "လက်ရှိ",
                "ယခင်",
                "လာမည့်",
                "ပိတ်ထားသည်"
            };

        // =========================================================
        // PAGINATION
        // =========================================================

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalFilteredCount;

        public ObservableCollection<int> PageSizeOptions { get; } =
            new()
            {
                10,
                20,
                50,
                100
            };

        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public bool HasFeeRules => FeeRules.Count > 0;

        public bool HasNoFeeRules => FeeRules.Count == 0;

        public FeeRuleViewModel()
        {
        }

        // =========================================================
        // LOAD FEE RULES
        // =========================================================

        [RelayCommand]
        private async Task LoadFeeRulesAsync()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;

                using var db = new AppDbContext();

                // Load ALL fee rules.
                //
                // We intentionally do NOT filter by IsActive here.
                // Historical and deactivated rules must remain visible.
                _allFeeRules = await db.FeeRules
                    .AsNoTracking()
                    .Include(fr => fr.StudentCategory)
                    .Include(fr => fr.RoomType)
                    .OrderBy(fr => fr.StudentCategory!.Name)
                    .ThenBy(fr => fr.RoomType!.Name)
                    .ThenByDescending(fr => fr.EffectiveFrom)
                    .ToListAsync();

                CurrentPage = 1;

                ApplyFiltersAndPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"အခန်းခနှုန်းထားများကို ရယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================
        // FILTER
        // =========================================================

        [RelayCommand]
        private void ApplyFilter()
        {
            CurrentPage = 1;

            ApplyFiltersAndPagination();
        }

        [RelayCommand]
        private void ClearFilter()
        {
            FilterFromDate = null;
            FilterToDate = null;
            SelectedStatus = "အားလုံး";

            CurrentPage = 1;

            ApplyFiltersAndPagination();
        }

        private void ApplyFiltersAndPagination()
        {
            IEnumerable<FeeRule> filteredRules = _allFeeRules;

            // =====================================================
            // DATE RANGE FILTER
            // =====================================================
            //
            // Show rules whose effective period overlaps
            // the selected date range.
            //
            // Example:
            //
            // Rule:
            // 01/06/2026 -> 31/08/2026
            //
            // Filter:
            // 01/07/2026 -> 31/07/2026
            //
            // Result:
            // Included
            // =====================================================

            if (FilterFromDate.HasValue)
            {
                var fromDate = FilterFromDate.Value.Date;

                filteredRules = filteredRules.Where(fr =>
                    !fr.EffectiveTo.HasValue ||
                    fr.EffectiveTo.Value.Date >= fromDate);
            }

            if (FilterToDate.HasValue)
            {
                var toDate = FilterToDate.Value.Date;

                filteredRules = filteredRules.Where(fr =>
                    fr.EffectiveFrom.Date <= toDate);
            }

            // =====================================================
            // STATUS FILTER
            // =====================================================

            if (SelectedStatus != "အားလုံး")
            {
                filteredRules = filteredRules.Where(fr =>
                    GetStatus(fr) == SelectedStatus);
            }

            // =====================================================
            // SORT
            // =====================================================

            var filteredList = filteredRules
                .OrderBy(fr => fr.StudentCategory?.Name)
                .ThenBy(fr => fr.RoomType?.Name)
                .ThenByDescending(fr => fr.EffectiveFrom)
                .ToList();

            TotalFilteredCount = filteredList.Count;

            // =====================================================
            // CALCULATE TOTAL PAGES
            // =====================================================

            TotalPages = TotalFilteredCount == 0
                ? 1
                : (int)Math.Ceiling(
                    TotalFilteredCount / (double)PageSize);

            if (CurrentPage < 1)
                CurrentPage = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;

            // =====================================================
            // APPLY PAGINATION
            // =====================================================

            var pagedRules = filteredList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            FeeRules.Clear();

            foreach (var feeRule in pagedRules)
            {
                FeeRules.Add(feeRule);
            }

            NotifyPaginationProperties();
        }

        // =========================================================
        // STATUS
        // =========================================================

        public static string GetStatus(FeeRule feeRule)
        {
            // Manually deactivated always takes priority.
            if (!feeRule.IsActive)
                return "ပိတ်ထားသည်";

            var today = DateTime.Today;

            // Rule starts in the future.
            if (feeRule.EffectiveFrom.Date > today)
                return "လာမည့်";

            // Rule has already ended.
            if (feeRule.EffectiveTo.HasValue &&
                feeRule.EffectiveTo.Value.Date < today)
            {
                return "ယခင်";
            }

            // Currently effective.
            return "လက်ရှိ";
        }

        // =========================================================
        // FIRST PAGE
        // =========================================================

        [RelayCommand]
        private void FirstPage()
        {
            if (CurrentPage == 1)
                return;

            CurrentPage = 1;

            ApplyFiltersAndPagination();
        }

        // =========================================================
        // PREVIOUS PAGE
        // =========================================================

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage <= 1)
                return;

            CurrentPage--;

            ApplyFiltersAndPagination();
        }

        // =========================================================
        // NEXT PAGE
        // =========================================================

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage >= TotalPages)
                return;

            CurrentPage++;

            ApplyFiltersAndPagination();
        }

        // =========================================================
        // LAST PAGE
        // =========================================================

        [RelayCommand]
        private void LastPage()
        {
            if (CurrentPage >= TotalPages)
                return;

            CurrentPage = TotalPages;

            ApplyFiltersAndPagination();
        }

        // =========================================================
        // PAGE SIZE CHANGED
        // =========================================================

        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0)
                return;

            CurrentPage = 1;

            ApplyFiltersAndPagination();
        }

        // =========================================================
        // FILTER PROPERTY CHANGES
        // =========================================================

        partial void OnSelectedStatusChanged(string value)
        {
            CurrentPage = 1;

            if (_allFeeRules.Count > 0)
            {
                ApplyFiltersAndPagination();
            }
        }

        partial void OnFilterFromDateChanged(DateTime? value)
        {
            CurrentPage = 1;

            if (_allFeeRules.Count > 0)
            {
                ApplyFiltersAndPagination();
            }
        }

        partial void OnFilterToDateChanged(DateTime? value)
        {
            CurrentPage = 1;

            if (_allFeeRules.Count > 0)
            {
                ApplyFiltersAndPagination();
            }
        }

        // =========================================================
        // DEACTIVATE FEE RULE
        // =========================================================

        [RelayCommand]
        private async Task DeleteFeeRuleAsync(FeeRule? feeRule)
        {
            if (feeRule == null || IsLoading)
                return;

            // Already deactivated.
            if (!feeRule.IsActive)
                return;

            var result = MessageBox.Show(
                $"'{feeRule.StudentCategory?.Name} - {feeRule.RoomType?.Name}' အခန်းခနှုန်းထားကို ပိတ်ရန် သေချာပါသလား?\n\n" +
                "ပိတ်ပြီးနောက် ဤနှုန်းထားကို အသစ်ထပ်မံအသုံးပြု၍မရတော့ပါ။\n" +
                "ရှိပြီးသား အခန်းခကျသင့်ငွေများကို မထိခိုက်စေပါ။",
                "အခန်းခနှုန်းထား ပိတ်ရန် အတည်ပြုပါ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                using var db = new AppDbContext();

                var existingFeeRule = await db.FeeRules
                    .FirstOrDefaultAsync(fr => fr.Id == feeRule.Id);

                if (existingFeeRule == null)
                {
                    MessageBox.Show(
                        "အခန်းခနှုန်းထားကို မတွေ့ရှိပါ။",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                existingFeeRule.IsActive = false;

                await db.SaveChangesAsync();

                // Update the local copy.
                var localRule = _allFeeRules
                    .FirstOrDefault(fr => fr.Id == existingFeeRule.Id);

                if (localRule != null)
                {
                    localRule.IsActive = false;
                }

                // Refresh the current filtered/paginated list.
                ApplyFiltersAndPagination();

                WeakReferenceMessenger.Default.Send(
                    new DatabaseUpdatedMessage(
                        "FeeRule",
                        existingFeeRule.Id,
                        DatabaseOperation.Deleted));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"အခန်းခနှုန်းထားကို ပိတ်ရာတွင် အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================
        // CREATE FEE RULE
        // =========================================================

        [RelayCommand]
        private async Task OpenCreateFeeRuleFormAsync()
        {
            if (IsLoading)
                return;

            var formViewModel = new FeeRuleFormViewModel(this);

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(formViewModel));

            await formViewModel.InitializeAsync();
        }

        // =========================================================
        // PROPERTY NOTIFICATIONS
        // =========================================================

        private void NotifyPaginationProperties()
        {
            OnPropertyChanged(nameof(HasFeeRules));
            OnPropertyChanged(nameof(HasNoFeeRules));
            OnPropertyChanged(nameof(TotalFilteredCount));
            OnPropertyChanged(nameof(TotalPages));
        }
    }
}
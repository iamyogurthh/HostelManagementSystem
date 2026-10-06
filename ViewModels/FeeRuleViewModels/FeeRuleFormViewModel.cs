using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace HostelManagementSystem.ViewModels.FeeRuleViewModels
{
    public partial class FeeRuleFormViewModel : ObservableObject
    {
        private readonly FeeRuleViewModel _previousViewModel;

        // =========================================================
        // FORM PROPERTIES
        // =========================================================

        [ObservableProperty]
        private string _title = "အခန်းခနှုန်းထားအသစ်";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<StudentCategory> _studentCategories = new();

        [ObservableProperty]
        private ObservableCollection<RoomType> _roomTypes = new();

        [ObservableProperty]
        private StudentCategory? _selectedStudentCategory;

        [ObservableProperty]
        private RoomType? _selectedRoomType;

        [ObservableProperty]
        private decimal _monthlyAmount;

        [ObservableProperty]
        private DateTime _effectiveFrom = DateTime.Today;

        [ObservableProperty]
        private DateTime? _effectiveTo;

        public FeeRuleFormViewModel(
            FeeRuleViewModel previousViewModel)
        {
            _previousViewModel = previousViewModel;
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        public async Task InitializeAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                using var db = new AppDbContext();

                var categories = await db.StudentCategories
                    .AsNoTracking()
                    .Where(sc => sc.IsActive)
                    .OrderBy(sc => sc.Name)
                    .ToListAsync();

                StudentCategories.Clear();

                foreach (var category in categories)
                {
                    StudentCategories.Add(category);
                }

                var roomTypes = await db.RoomTypes
                    .AsNoTracking()
                    .Where(rt => rt.IsActive)
                    .OrderBy(rt => rt.Name)
                    .ToListAsync();

                RoomTypes.Clear();

                foreach (var roomType in roomTypes)
                {
                    RoomTypes.Add(roomType);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အချက်အလက်များကို ရယူရာတွင် အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n{ex.Message}";
            }
        }

        // =========================================================
        // SAVE
        // =========================================================

        [RelayCommand]
        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;

            // -----------------------------------------------------
            // Student Category validation
            // -----------------------------------------------------

            if (SelectedStudentCategory == null)
            {
                ErrorMessage = "ကျောင်းသားအမျိုးအစားကို ရွေးချယ်ပါ။";
                return;
            }

            // -----------------------------------------------------
            // Room Type validation
            // -----------------------------------------------------

            if (SelectedRoomType == null)
            {
                ErrorMessage = "အခန်းအမျိုးအစားကို ရွေးချယ်ပါ။";
                return;
            }

            // -----------------------------------------------------
            // Monthly amount validation
            // -----------------------------------------------------

            if (MonthlyAmount <= 0)
            {
                ErrorMessage = "လစဉ်ကြေးသည် 0 ထက်ကြီးရပါမည်။";
                return;
            }

            // -----------------------------------------------------
            // Date validation
            // -----------------------------------------------------

            if (EffectiveTo.HasValue &&
                EffectiveTo.Value.Date < EffectiveFrom.Date)
            {
                ErrorMessage =
                    "အဆုံးသတ်အကျိုးသက်ရောက်သည့်နေ့သည် စတင်အကျိုးသက်ရောက်သည့်နေ့ထက် မစောရပါ။";

                return;
            }

            try
            {
                using var db = new AppDbContext();

                // -------------------------------------------------
                // Verify Student Category still exists and active
                // -------------------------------------------------

                var categoryExists = await db.StudentCategories
                    .AnyAsync(sc =>
                        sc.Id == SelectedStudentCategory.Id &&
                        sc.IsActive);

                if (!categoryExists)
                {
                    ErrorMessage =
                        "ရွေးချယ်ထားသော ကျောင်းသားအမျိုးအစားသည် မရှိတော့ပါ။";

                    return;
                }

                // -------------------------------------------------
                // Verify Room Type still exists and active
                // -------------------------------------------------

                var roomTypeExists = await db.RoomTypes
                    .AnyAsync(rt =>
                        rt.Id == SelectedRoomType.Id &&
                        rt.IsActive);

                if (!roomTypeExists)
                {
                    ErrorMessage =
                        "ရွေးချယ်ထားသော အခန်းအမျိုးအစားသည် မရှိတော့ပါ။";

                    return;
                }

                // -------------------------------------------------
                // Check overlapping active fee rules
                // -------------------------------------------------

                bool hasOverlap = await db.FeeRules
                    .AnyAsync(fr =>
                        fr.IsActive &&
                        fr.StudentCategoryId == SelectedStudentCategory.Id &&
                        fr.RoomTypeId == SelectedRoomType.Id &&

                        // Existing rule starts before the new rule ends.
                        (!EffectiveTo.HasValue ||
                         fr.EffectiveFrom.Date <= EffectiveTo.Value.Date) &&

                        // Existing rule ends after the new rule starts.
                        (!fr.EffectiveTo.HasValue ||
                         fr.EffectiveTo.Value.Date >= EffectiveFrom.Date));

                if (hasOverlap)
                {
                    ErrorMessage =
                        "ရွေးချယ်ထားသော ကျောင်းသားအမျိုးအစားနှင့် အခန်းအမျိုးအစားအတွက် သတ်မှတ်ထားသော ရက်အတွင်း အခန်းခနှုန်းထားတစ်ခု ရှိပြီးသားဖြစ်ပါသည်။";

                    return;
                }

                // -------------------------------------------------
                // Create new FeeRule
                // -------------------------------------------------

                await CreateFeeRuleAsync(db);
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage =
                    $"ဒေတာသိမ်းဆည်းရာတွင် Database အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n" +
                    $"{ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"အမှားဖြစ်ပေါ်ခဲ့ပါသည်။\n{ex.Message}";
            }
        }

        // =========================================================
        // CREATE FEE RULE
        // =========================================================

        private async Task CreateFeeRuleAsync(AppDbContext db)
        {
            var feeRule = new FeeRule
            {
                StudentCategoryId = SelectedStudentCategory!.Id,
                RoomTypeId = SelectedRoomType!.Id,
                MonthlyAmount = MonthlyAmount,
                EffectiveFrom = EffectiveFrom.Date,
                EffectiveTo = EffectiveTo?.Date,
                IsActive = true
            };

            db.FeeRules.Add(feeRule);

            await db.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(
                new DatabaseUpdatedMessage(
                    "FeeRule",
                    feeRule.Id,
                    DatabaseOperation.Inserted));

            await ReturnToFeeRuleListAsync();
        }

        // =========================================================
        // CANCEL
        // =========================================================

        [RelayCommand]
        private async Task CancelAsync()
        {
            await ReturnToFeeRuleListAsync();
        }

        // =========================================================
        // RETURN TO FEE RULE LIST
        // =========================================================

        private async Task ReturnToFeeRuleListAsync()
        {
            await _previousViewModel.LoadFeeRulesCommand.ExecuteAsync(null);

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(_previousViewModel));
        }
    }
}
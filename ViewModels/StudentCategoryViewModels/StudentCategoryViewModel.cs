using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using HostelManagementSystem.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels.StudentCategoryViewModels
{
    public partial class StudentCategoryViewModel : ObservableObject
    {
        // Callback used to return to the ViewModel that opened this page.
        private readonly Func<Task> _navigateBackCallback;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCategories))]
        [NotifyPropertyChangedFor(nameof(HasNoCategories))]
        private ObservableCollection<StudentCategory> _categories = new();

        public bool HasCategories => Categories.Count > 0;

        public bool HasNoCategories => Categories.Count == 0;

        public StudentCategoryViewModel(
            Func<Task> navigateBackCallback)
        {
            _navigateBackCallback = navigateBackCallback
                ?? throw new ArgumentNullException(nameof(navigateBackCallback));

            Categories.CollectionChanged += (s, e) =>
                NotifyStateChanges();
        }

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(HasCategories));
            OnPropertyChanged(nameof(HasNoCategories));
        }

        // ============================================================
        // LOAD CATEGORIES
        // ============================================================

        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                await using var db = new AppDbContext();

                var categoryList = await db.StudentCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                Categories.Clear();

                foreach (var category in categoryList)
                {
                    Categories.Add(category);
                }

                NotifyStateChanges();

            }, "ကျောင်းသားအမျိုးအစားများ ရယူနေပါသည်...");
        }

        // ============================================================
        // CREATE
        // ============================================================

        [RelayCommand]
        private void OpenCreateCategoryForm()
        {
            var formViewModel = new StudentCategoryFormViewModel(
                async () =>
                {
                    await LoadCategoriesAsync();

                    WeakReferenceMessenger.Default.Send(
                        new NavigateMessage(this));
                });

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(formViewModel));
        }

        // ============================================================
        // EDIT
        // ============================================================
        [RelayCommand]
        private void OpenEditCategoryForm(StudentCategory? category)
        {
            if (category == null)
                return;

            var formViewModel = new StudentCategoryFormViewModel(
                async () =>
                {
                    await LoadCategoriesAsync();

                    WeakReferenceMessenger.Default.Send(
                        new NavigateMessage(this));
                },
                category);

            WeakReferenceMessenger.Default.Send(
                new NavigateMessage(formViewModel));
        }

        // ============================================================
        // DELETE
        // ============================================================

        [RelayCommand]
        private async Task DeleteCategoryAsync(
            StudentCategory? category)
        {
            if (category == null)
                return;

            try
            {
                await using var db = new AppDbContext();

                // Prevent deletion if active students use this category.
                bool isCategoryUsedByStudents =
                    await db.Students.AnyAsync(s =>
                        s.StudentCategoryId == category.Id &&
                        s.IsActive);

                if (isCategoryUsedByStudents)
                {
                    MessageBox.Show(
                        $"\"{category.Name}\" ကို ကျောင်းသားများ အသုံးပြုနေဆဲ ဖြစ်သောကြောင့် ဖျက်၍မရပါ။",
                        "ဖျက်၍ မရပါ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Prevent deletion if an active fee rule uses this category.
                bool isCategoryUsedByFeeRule =
                    await db.FeeRules.AnyAsync(fr =>
                        fr.StudentCategoryId == category.Id &&
                        fr.IsActive);

                if (isCategoryUsedByFeeRule)
                {
                    MessageBox.Show(
                        $"\"{category.Name}\" ကို အခကြေးငွေ စည်းမျဉ်းများတွင် အသုံးပြုနေဆဲ ဖြစ်သောကြောင့် ဖျက်၍မရပါ။",
                        "ဖျက်၍ မရပါ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"စစ်ဆေးရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var result = MessageBox.Show(
                $"\"{category.Name}\" ကျောင်းသားအမျိုးအစားကို ဖျက်ရန် သေချာပါသလား။",
                "အတည်ပြုရန်",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await LoadingHelper.ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    await using var db = new AppDbContext();

                    var categoryToUpdate =
                        await db.StudentCategories
                            .FirstOrDefaultAsync(c =>
                                c.Id == category.Id);

                    if (categoryToUpdate == null)
                        return;

                    // Soft delete
                    categoryToUpdate.IsActive = false;

                    await db.SaveChangesAsync();

                    Categories.Remove(category);

                    WeakReferenceMessenger.Default.Send(
                        new DatabaseUpdatedMessage(
                            "StudentCategory",
                            category.Id,
                            DatabaseOperation.Deleted));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"ဖျက်ရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                        "အမှား",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

            }, "ကျောင်းသားအမျိုးအစားဖျက်နေပါသည်...");
        }

        // ============================================================
        // BACK
        // ============================================================

        [RelayCommand]
        private async Task BackAsync()
        {
            await _navigateBackCallback();
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HostelManagementSystem.Database;
using HostelManagementSystem.Messages;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels.StudentCategoryViewModels
{
    public partial class StudentCategoryFormViewModel : ObservableObject
    {
        private readonly Func<Task> _navigateBackCallback;
        private readonly int? _categoryId;

        public bool IsEditMode => _categoryId.HasValue;

        public string Title =>
            IsEditMode
                ? "အမျိုးအစား ပြင်ဆင်ရန်"
                : "ကျောင်းသား အမျိုးအစား အသစ်";

        [ObservableProperty]
        private string _name = string.Empty;

        partial void OnNameChanged(string value)
        {
            ErrorMessage = string.Empty;
        }

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Create
        public StudentCategoryFormViewModel(
            Func<Task> navigateBackCallback)
        {
            _navigateBackCallback = navigateBackCallback
                ?? throw new ArgumentNullException(nameof(navigateBackCallback));
        }

        // Edit
        public StudentCategoryFormViewModel(
            Func<Task> navigateBackCallback,
            StudentCategory category)
        {
            _navigateBackCallback = navigateBackCallback
                ?? throw new ArgumentNullException(nameof(navigateBackCallback));

            if (category == null)
                throw new ArgumentNullException(nameof(category));

            _categoryId = category.Id;
            Name = category.Name;
            Description = category.Description;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "အမျိုးအစား အမည် ထည့်သွင်းပါ။";
                return;
            }

            // Clean input
            Name = Name.Trim();

            Description = string.IsNullOrWhiteSpace(Description)
                ? null
                : Description.Trim();

            try
            {
                using var db = new AppDbContext();

                // Check duplicate active category
                bool duplicateExists = await db.StudentCategories
                    .AnyAsync(c =>
                        c.IsActive &&
                        c.Name.ToLower() == Name.ToLower() &&
                        (!_categoryId.HasValue ||
                         c.Id != _categoryId.Value));

                if (duplicateExists)
                {
                    ErrorMessage =
                        "ဤကျောင်းသား အမျိုးအစား အမည် ရှိပြီးသား ဖြစ်ပါသည်။";

                    return;
                }

                // Edit existing category
                if (_categoryId is int categoryId)
                {
                    var category = await db.StudentCategories
                        .FirstOrDefaultAsync(c => c.Id == categoryId);

                    if (category == null)
                    {
                        MessageBox.Show(
                            "ကျောင်းသား အမျိုးအစားကို ရှာမတွေ့ပါ။",
                            "အမှား",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return;
                    }

                    if (!category.IsActive)
                    {
                        MessageBox.Show(
                            "ဤကျောင်းသား အမျိုးအစားသည် အသုံးမပြုတော့ပါ။",
                            "အမှား",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    category.Name = Name;
                    category.Description = Description;
                }
                // Create new category
                else
                {
                    var category = new StudentCategory
                    {
                        Name = Name,
                        Description = Description,
                        IsActive = true
                    };

                    db.StudentCategories.Add(category);
                }

                await db.SaveChangesAsync();

                // Navigate back and reload the category list
                await _navigateBackCallback();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"သိမ်းဆည်းရာတွင် အမှားတစ်ခု ဖြစ်ပေါ်ခဲ့သည်:\n{ex.Message}",
                    "အမှား",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await _navigateBackCallback();
        }
    }
}
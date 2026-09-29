using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HostelManagementSystem.ViewModels.StudentViewModels
{
    public partial class BirthdayInputViewModel : ObservableObject
    {
        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<int> Months { get; } = new();
        public ObservableCollection<int> Days { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedBirthDate))]
        [NotifyPropertyChangedFor(nameof(Age))]
        [NotifyPropertyChangedFor(nameof(SelectedDate))]
        private int _selectedYear;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedBirthDate))]
        [NotifyPropertyChangedFor(nameof(Age))]
        [NotifyPropertyChangedFor(nameof(SelectedDate))]
        private int _selectedMonth;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedBirthDate))]
        [NotifyPropertyChangedFor(nameof(Age))]
        [NotifyPropertyChangedFor(nameof(SelectedDate))]
        private int _selectedDay;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public BirthdayInputViewModel()
        {
            InitializeDataSources();
        }

        private void InitializeDataSources()
        {
            int currentYear = DateTime.Now.Year;

            // Populate Years (e.g., past 100 years to current year)
            Years.Clear();
            for (int y = currentYear; y >= currentYear - 100; y--)
            {
                Years.Add(y);
            }

            // Populate Months (1 to 12)
            Months.Clear();
            for (int m = 1; m <= 12; m++)
            {
                Months.Add(m);
            }

            // Set Default Selections (Default to 18 years ago)
            SelectedYear = currentYear - 18;
            SelectedMonth = 1;
            UpdateDaysInMonth();
            SelectedDay = 1;
        }

        partial void OnSelectedYearChanged(int value)
        {
            UpdateDaysInMonth();
        }

        partial void OnSelectedMonthChanged(int value)
        {
            UpdateDaysInMonth();
        }

        private void UpdateDaysInMonth()
        {
            if (SelectedYear <= 0 || SelectedMonth <= 0 || SelectedMonth > 12) return;

            // FIX: Replaced non-existent DateTime.DaysInWeekCount with standard DateTime.DaysInMonth
            int daysInMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

            int currentSelectedDay = SelectedDay;

            Days.Clear();
            for (int d = 1; d <= daysInMonth; d++)
            {
                Days.Add(d);
            }

            // Preserve selected day if still valid, otherwise clamp to last day of month
            SelectedDay = currentSelectedDay > daysInMonth ? daysInMonth : (currentSelectedDay == 0 ? 1 : currentSelectedDay);
        }

        public bool Validate(string label = "Birthday")
        {
            try
            {
                var birthDate = new DateTime(SelectedYear, SelectedMonth, SelectedDay);
                if (birthDate > DateTime.Today)
                {
                    ErrorMessage = $"{label} cannot be in the future.";
                    return false;
                }

                ErrorMessage = string.Empty;
                return true;
            }
            catch
            {
                ErrorMessage = $"Invalid {label} selected.";
                return false;
            }
        }

        public void SetDate(DateTime? date)
        {
            if (!date.HasValue || date.Value == DateTime.MinValue)
            {
                ErrorMessage = string.Empty;
                return;
            }

            // Setting these automatically triggers property notifications 
            // and updates UpdateDaysInMonth() & SelectedDate
            SelectedYear = date.Value.Year;
            SelectedMonth = date.Value.Month;
            SelectedDay = date.Value.Day;
        }

        public DateTime? SelectedDate
        {
            get
            {
                try
                {
                    return new DateTime(SelectedYear, SelectedMonth, SelectedDay);
                }
                catch
                {
                    return null;
                }
            }
        }

        public string FormattedBirthDate => SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;

        // FIX: Removed invalid semicolon after "get"
        public int Age
        {
            get
            {
                if (!SelectedDate.HasValue) return 0;

                var today = DateTime.Today;
                var birthDate = SelectedDate.Value;
                int age = today.Year - birthDate.Year;

                if (birthDate.Date > today.AddYears(-age))
                {
                    age--;
                }

                return age < 0 ? 0 : age;
            }
        }
    }
}
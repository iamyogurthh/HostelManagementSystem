using HostelManagementSystem.Models;
using HostelManagementSystem.ViewModels.FeeRuleViewModels;
using System.Globalization;
using System.Windows.Data;

namespace HostelManagementSystem.Converters
{
    public class FeeRuleStatusConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is not FeeRule feeRule)
                return string.Empty;

            var status = FeeRuleViewModel.GetStatus(feeRule);

            return parameter?.ToString() switch
            {
                "Background" => GetBackground(status),
                "Foreground" => GetForeground(status),
                _ => status
            };
        }

        private static string GetBackground(string status)
        {
            return status switch
            {
                "လက်ရှိ" => "#ECFDF5",
                "ယခင်" => "#F1F5F9",
                "လာမည့်" => "#EFF6FF",
                "ပိတ်ထားသည်" => "#FEF2F2",
                _ => "#F1F5F9"
            };
        }

        private static string GetForeground(string status)
        {
            return status switch
            {
                "လက်ရှိ" => "#059669",
                "ယခင်" => "#64748B",
                "လာမည့်" => "#2563EB",
                "ပိတ်ထားသည်" => "#DC2626",
                _ => "#64748B"
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
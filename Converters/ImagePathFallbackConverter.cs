using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace HostelManagementSystem.Converters;

public sealed class ImagePathFallbackConverter : IValueConverter
{
    private const string DefaultRoomImage = "pack://application:,,,/Image/DefaultImages/default_room.png";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string imagePath || string.IsNullOrWhiteSpace(imagePath))
        {
            return DefaultRoomImage;
        }

        if (imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase) ||
            File.Exists(imagePath))
        {
            return imagePath;
        }

        return DefaultRoomImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

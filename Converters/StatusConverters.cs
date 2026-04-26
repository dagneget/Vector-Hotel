using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HRS.Converters
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "ACTIVE" : "INACTIVE";
            return "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToSuccessDangerBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                var resourceName = b ? "SuccessGreenBrush" : "DangerRedBrush";
                return Application.Current.Resources[resourceName] as Brush ?? (b ? Brushes.Green : Brushes.Red);
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                if (parameter is Brush brush) return brush;
                if (parameter is string resourceKey) return Application.Current.Resources[resourceKey] as Brush;
                return Brushes.Blue; // Default
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts boolean to opacity value (for enabling/disabling UI elements visually)
    /// </summary>
    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 1.0 : 0.4;
            return 0.4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Multi-value converter that returns true only if all bound values are non-null/non-empty
    /// Used for enabling buttons when all required fields are filled
    /// </summary>
    public class AllValuesFilledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            return values.All(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts reservation status string to a color brush
    /// </summary>
    public class StatusToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "CheckedIn":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "CheckedOut":
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    case "Confirmed":
                        return new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                    case "Pending":
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    case "Cancelled":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    case "Occupied":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "Available":
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts string to Visibility - returns Visible if string is not null or empty, Collapsed otherwise
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

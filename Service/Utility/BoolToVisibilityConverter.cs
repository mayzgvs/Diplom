using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Service.Utility
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Если параметр "Inverse" передан, инвертируем значение
                if (parameter?.ToString() == "Inverse")
                    boolValue = !boolValue;

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Если параметр "Inverse" передан, инвертируем значение
                if (parameter?.ToString() == "Inverse")
                    return visibility != Visibility.Visible;

                return visibility == Visibility.Visible;
            }

            return false;
        }
    }
}
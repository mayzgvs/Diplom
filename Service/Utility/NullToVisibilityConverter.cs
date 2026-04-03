using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Service.Utility
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (string.IsNullOrWhiteSpace(str) ||
                str == "Не указан" ||
                str == "Не указана")
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Service.Utility
{
    /// <summary>
    /// Конвертер: пустая строка → Collapsed, любая непустая строка → Visible
    /// Используется для скрытия/показа сообщений об ошибках (например DateError)
    /// </summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack не поддерживается.");
        }
    }
}
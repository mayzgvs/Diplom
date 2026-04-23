using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Service.Utility
{
    public class ColorToMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color drawingColor)
            {
                return Color.FromArgb(
                    drawingColor.A,
                    drawingColor.R,
                    drawingColor.G,
                    drawingColor.B);
            }

            if (value is Color mediaColor)
                return mediaColor;

            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
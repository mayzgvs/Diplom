using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Service.Utility
{
    public class PhoneTextBox : TextBox
    {
        private bool _isFormatting = false;

        public PhoneTextBox()
        {
            VerticalContentAlignment = VerticalAlignment.Center;
            FontSize = 14;
            Padding = new Thickness(8, 2, 8, 2);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            // Только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
            base.OnPreviewTextInput(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (_isFormatting) return;

            _isFormatting = true;

            // Получаем только цифры
            string digits = new string(Text.Where(char.IsDigit).ToArray());

            // Ограничение 11 цифр
            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            // Форматируем
            string formatted = FormatPhoneNumber(digits);

            // Применяем
            Text = formatted;
            CaretIndex = formatted.Length;

            _isFormatting = false;

            base.OnTextChanged(e);
        }

        private string FormatPhoneNumber(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return "";

            string result = "+7(";

            // +7(XXX
            if (digits.Length >= 1)
                result += digits.Substring(0, Math.Min(3, digits.Length));

            if (digits.Length <= 3) return result;

            // )XXX
            result += ")";
            result += digits.Substring(3, Math.Min(3, digits.Length - 3));

            if (digits.Length <= 6) return result;

            // -XX
            result += "-";
            result += digits.Substring(6, Math.Min(2, digits.Length - 6));

            if (digits.Length <= 8) return result;

            // -XX
            result += "-";
            result += digits.Substring(8, Math.Min(2, digits.Length - 8));

            return result;
        }
    }
}
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Service.Utility
{
    public class PhoneTextBox : TextBox
    {
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(PhoneTextBox),
                new PropertyMetadata("+7 (___) ___-__-__"));

        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        private string _previousText = "";

        public PhoneTextBox()
        {
            VerticalContentAlignment = VerticalAlignment.Center;
            TextAlignment = TextAlignment.Center;

            // Подключаем обработку вставки (Ctrl+V)
            CommandManager.AddPreviewExecutedHandler(this, OnPreviewExecuted);
        }

        // ==================== Только цифры при вводе ====================
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
            base.OnPreviewTextInput(e);
        }

        // ==================== Клавиши ====================
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            if (e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            e.Handled = true;
        }

        // ==================== Обработка вставки Ctrl+V ====================
        private void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                string clipboardText = Clipboard.GetText() ?? "";
                string digitsOnly = new string(clipboardText.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrEmpty(digitsOnly))
                {
                    string currentDigits = GetOnlyDigits(Text);
                    string newDigits = currentDigits + digitsOnly;

                    if (newDigits.Length > 11)
                        newDigits = newDigits.Substring(0, 11);

                    string formatted = ApplyMask(newDigits);

                    Text = formatted;
                    CaretIndex = formatted.Length;

                    e.Handled = true;        // блокируем стандартную вставку
                    return;
                }

                // Если в буфере нет цифр — отменяем вставку
                e.Handled = true;
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            if (Text == _previousText)
                return;

            string digits = GetOnlyDigits(Text);

            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            string newFormatted = ApplyMask(digits);

            if (newFormatted != Text)
            {
                int oldCaret = CaretIndex;
                _previousText = newFormatted;
                Text = newFormatted;
                CaretIndex = CalculateCaretPosition(_previousText, newFormatted, oldCaret);
            }
            else
            {
                _previousText = Text;
            }
        }

        private string GetOnlyDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return new string(text.Where(char.IsDigit).ToArray());
        }

        private string ApplyMask(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return Mask.Replace("_", "");

            string result = Mask;
            int digitIndex = 0;

            for (int i = 0; i < result.Length && digitIndex < digits.Length; i++)
            {
                if (result[i] == '_')
                {
                    result = result.Remove(i, 1).Insert(i, digits[digitIndex].ToString());
                    digitIndex++;
                }
            }

            int underscoreIndex = result.IndexOf('_');
            if (underscoreIndex >= 0)
                result = result.Substring(0, underscoreIndex);

            return result;
        }

        private int CalculateCaretPosition(string oldFormatted, string newFormatted, int oldCaret)
        {
            int oldDigitCount = GetOnlyDigits(oldFormatted).Length;
            int newDigitCount = GetOnlyDigits(newFormatted).Length;

            if (newDigitCount < oldDigitCount) // удаление
            {
                int pos = Math.Max(0, oldCaret - 1);
                while (pos > 0 && !char.IsDigit(newFormatted[pos]))
                    pos--;
                return pos;
            }

            // добавление — курсор в конец
            return newFormatted.Length;
        }

        // Очистка обработчика при уничтожении элемента
        ~PhoneTextBox()
        {
            CommandManager.RemovePreviewExecutedHandler(this, OnPreviewExecuted);
        }
    }
}
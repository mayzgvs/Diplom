using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Service.Views
{
    public partial class CustomMessageBox : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;

        public CustomMessageBox(string message, string title = "Сообщение",
                           MessageBoxButton buttons = MessageBoxButton.OK,
                           MessageBoxImage icon = MessageBoxImage.None)
        {
            InitializeComponent();

            txtTitle.Text = title;
            txtMessage.Text = message;

            SetupIcon(icon);
            SetupButtons(buttons);

            Owner = Application.Current.MainWindow;

            // Обновляем размер после загрузки
            this.Loaded += (s, e) =>
            {
                // Принудительно обновляем Layout
                this.InvalidateVisual();

                // Ограничиваем максимальный размер
                if (this.ActualHeight > MaxHeight)
                    this.Height = MaxHeight;
                if (this.ActualWidth > MaxWidth)
                    this.Width = MaxWidth;
            };
        }

        private void SetupIcon(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Information:
                    IconPath.Data = (Geometry)FindResource("InfoIcon");
                    break;
                case MessageBoxImage.Question:
                    IconPath.Data = (Geometry)FindResource("QuestionIcon");
                    break;
                case MessageBoxImage.Warning:
                    IconPath.Data = (Geometry)FindResource("WarningIcon");
                    break;
                case MessageBoxImage.Error:
                    IconPath.Data = (Geometry)FindResource("ErrorIcon");
                    break;
                default:
                    IconPath.Data = (Geometry)FindResource("InfoIcon");
                    break;
            }
        }

        private void SetupButtons(MessageBoxButton buttons)
        {
            ButtonsPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, true, "DialogButtonStyle");
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton("Отмена", MessageBoxResult.Cancel, false, "DialogSecondaryButtonStyle");
                    AddButton("OK", MessageBoxResult.OK, true, "DialogButtonStyle");
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("Нет", MessageBoxResult.No, false, "DialogButtonStyle");
                    AddButton("Да", MessageBoxResult.Yes, true, "DialogButtonStyle");
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("Отмена", MessageBoxResult.Cancel, false, "DialogSecondaryButtonStyle");
                    AddButton("Нет", MessageBoxResult.No, false, "DialogButtonStyle");
                    AddButton("Да", MessageBoxResult.Yes, true, "DialogButtonStyle");
                    break;
            }
        }

        private void AddButton(string text, MessageBoxResult result, bool isDefault = false, string styleKey = "DialogButtonStyle")
        {
            var btn = new Button
            {
                Content = text,
                Style = (Style)FindResource(styleKey)
            };

            if (isDefault)
            {
                btn.IsDefault = true;
                btn.Focus();
            }

            btn.Click += (s, e) =>
            {
                _result = result;
                DialogResult = true;
                Close();
            };

            ButtonsPanel.Children.Add(btn);
        }

        // Статический метод для удобного вызова из ViewModel
        public static MessageBoxResult Show(string message,
                                           string title = "Сообщение",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            var dlg = new CustomMessageBox(message, title, buttons, icon);
            dlg.ShowDialog();
            return dlg._result;
        }

        // Перегрузка для вызова с владельцем
        public static MessageBoxResult Show(Window owner, string message,
                                           string title = "Сообщение",
                                           MessageBoxButton buttons = MessageBoxButton.OK,
                                           MessageBoxImage icon = MessageBoxImage.None)
        {
            var dlg = new CustomMessageBox(message, title, buttons, icon);
            dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg._result;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
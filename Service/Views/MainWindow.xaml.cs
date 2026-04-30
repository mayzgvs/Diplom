using Service.ViewModels;
using Service.Views;        // ← Обязательно должна быть эта строка
using System;
using System.Diagnostics;
using System.Windows;

namespace Service.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;

            Debug.WriteLine($"DataContext установлен: {DataContext}");

            // Кнопки управления окном
            CloseButton.Click += (s, e) => Close();
            MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            MaximizeButton.Click += (s, e) =>
            {
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            };
        }

        // Метод для открытия настроек SMTP
        private void menuService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var smtpWindow = new SmtpSettingsWindow();
                smtpWindow.Owner = this;
                smtpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть окно настроек SMTP:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
using Service.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
            Debug.WriteLine($"NavigateCommand: {viewModel.NavigateCommand}");

            CloseButton.Click += (s, e) => Close();
            MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            MaximizeButton.Click += (s, e) =>
            {
                if (WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            };
        }
    }
}
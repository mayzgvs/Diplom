using Service.ViewModels;
using System.Windows;

namespace Service.Views
{
    public partial class SmtpSettingsWindow : Window
    {
        private readonly SmtpSettingsViewModel _viewModel;

        public SmtpSettingsWindow()
        {
            InitializeComponent();
            _viewModel = new SmtpSettingsViewModel();
            DataContext = _viewModel;
        }

        private void pbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SmtpPassword = pbPassword.Password;
        }
    }
}
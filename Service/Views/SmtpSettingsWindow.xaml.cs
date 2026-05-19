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

            // Важное исправление: загружаем сохранённый пароль в PasswordBox
            Loaded += SmtpSettingsWindow_Loaded;
        }

        private void SmtpSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_viewModel.SmtpPassword))
            {
                pbPassword.Password = _viewModel.SmtpPassword;
            }
        }

        private void pbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SmtpSettingsViewModel vm)
            {
                vm.SmtpPassword = pbPassword.Password;
            }
        }
    }
}
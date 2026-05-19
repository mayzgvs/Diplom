using Service.Views;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class SmtpSettingsViewModel : BaseViewModel
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string SmtpHost
        {
            get => Properties.Settings.Default.SmtpHost;
            set { Properties.Settings.Default.SmtpHost = value; OnPropertyChanged(); }
        }

        public int SmtpPort
        {
            get => Properties.Settings.Default.SmtpPort;
            set { Properties.Settings.Default.SmtpPort = value; OnPropertyChanged(); }
        }

        public bool SmtpEnableSsl
        {
            get => Properties.Settings.Default.SmtpEnableSsl;
            set { Properties.Settings.Default.SmtpEnableSsl = value; OnPropertyChanged(); }
        }

        public bool SmtpUseDefaultCredentials
        {
            get => Properties.Settings.Default.SmtpUseDefaultCredentials;
            set { Properties.Settings.Default.SmtpUseDefaultCredentials = value; OnPropertyChanged(); }
        }

        public string SmtpEmail
        {
            get => Properties.Settings.Default.SmtpEmail;
            set { Properties.Settings.Default.SmtpEmail = value; OnPropertyChanged(); }
        }

        public string SmtpPassword
        {
            get => Properties.Settings.Default.SmtpPassword;
            set { Properties.Settings.Default.SmtpPassword = value; OnPropertyChanged(); }
        }

        public string SmtpFromEmail
        {
            get => Properties.Settings.Default.SmtpFromEmail;
            set { Properties.Settings.Default.SmtpFromEmail = value; OnPropertyChanged(); }
        }

        public string SmtpSenderName
        {
            get => Properties.Settings.Default.SmtpSenderName;
            set { Properties.Settings.Default.SmtpSenderName = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand TestCommand { get; }
        public ICommand CancelCommand { get; }

        public SmtpSettingsViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            TestCommand = new RelayCommand(async _ => await TestConnectionAsync());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Save()
        {
            Properties.Settings.Default.Save();
            CustomMessageBox.Show("Настройки SMTP успешно сохранены!", "Сохранение",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestConnectionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // === СПЕЦИАЛЬНАЯ ПРОВЕРКА ПОРТА 587 ===
                if (SmtpPort == 587 && !SmtpEnableSsl)
                {
                    var result = CustomMessageBox.Show(
                        "Для порта 587 обычно требуется включить SSL/TLS (STARTTLS).\n\n" +
                        "Включить автоматически?",
                        "Рекомендация",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SmtpEnableSsl = true;
                        OnPropertyChanged(nameof(SmtpEnableSsl));
                    }
                }

                if (string.IsNullOrWhiteSpace(SmtpHost))
                {
                    CustomMessageBox.Show("Укажите SMTP сервер!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SmtpEmail))
                {
                    CustomMessageBox.Show("Укажите Email для входа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fromEmail = string.IsNullOrWhiteSpace(SmtpFromEmail) ? SmtpEmail : SmtpFromEmail;

                using (var smtpClient = new SmtpClient(SmtpHost, SmtpPort))
                {
                    smtpClient.EnableSsl = SmtpEnableSsl;
                    smtpClient.Timeout = 20000;

                    // Явная установка учётных данных
                    if (!SmtpUseDefaultCredentials &&
                        !string.IsNullOrWhiteSpace(SmtpEmail) &&
                        !string.IsNullOrWhiteSpace(SmtpPassword))
                    {
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(SmtpEmail, SmtpPassword);
                    }
                    else if (SmtpUseDefaultCredentials)
                    {
                        smtpClient.UseDefaultCredentials = true;
                    }

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail, SmtpSenderName ?? "Автосервис");
                        mailMessage.Subject = "Тест SMTP подключения";
                        mailMessage.Body = "Тестовое письмо от автосервиса.\n\nЕсли вы видите это — настройка прошла успешно!";
                        mailMessage.IsBodyHtml = false;
                        mailMessage.To.Add(SmtpEmail);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                CustomMessageBox.Show("✅ Подключение успешно!\nТестовое письмо отправлено.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SmtpException ex)
            {
                string message = $"Ошибка SMTP:\n{ex.Message}";
                if (ex.StatusCode == SmtpStatusCode.BadCommandSequence)
                {
                    message += "\n\nВозможные причины:\n" +
                               "• Неправильный пароль (используйте пароль приложения Яндекса)\n" +
                               "• Для порта 587 обязательно должен быть включён SSL/TLS";
                }
                CustomMessageBox.Show(message, "Ошибка SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка тестирования:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Cancel() { }
    }
}
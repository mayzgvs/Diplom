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
            MessageBox.Show("Настройки SMTP успешно сохранены!", "Сохранение",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                string fromEmail = string.IsNullOrWhiteSpace(SmtpFromEmail) ? SmtpEmail : SmtpFromEmail;

                if (string.IsNullOrWhiteSpace(SmtpHost))
                {
                    MessageBox.Show("Укажите SMTP сервер!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var smtpClient = new SmtpClient(SmtpHost, SmtpPort))
                {
                    smtpClient.EnableSsl = SmtpEnableSsl;
                    smtpClient.Timeout = 15000;

                    if (SmtpUseDefaultCredentials)
                    {
                        smtpClient.UseDefaultCredentials = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(SmtpEmail) && !string.IsNullOrWhiteSpace(SmtpPassword))
                    {
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(SmtpEmail, SmtpPassword);
                    }

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail, SmtpSenderName);
                        mailMessage.Subject = "Тест SMTP подключения";
                        mailMessage.Body = "Если вы видите это письмо — настройка SMTP прошла успешно!";
                        mailMessage.IsBodyHtml = false;
                        mailMessage.To.Add(SmtpEmail);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                MessageBox.Show("✅ Подключение успешно!\nТестовое письмо отправлено на ваш адрес.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка тестирования:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            // Логика закрытия окна будет в code-behind
        }
    }
}
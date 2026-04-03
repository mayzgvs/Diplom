using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;

namespace Service.Services
{
    public class EmailService
    {
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var settings = Properties.Settings.Default;

                string smtpHost = settings.SmtpHost ?? "smtp.yandex.ru";
                int smtpPort = settings.SmtpPort > 0 ? settings.SmtpPort : 587;
                string senderEmail = settings.SmtpEmail;
                string senderPassword = settings.SmtpPassword;
                string senderName = settings.SmtpSenderName ?? "Автосервис";

                if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    var result = MessageBox.Show(
                        "Email не настроен.\nХотите открыть настройки сейчас?",
                        "Настройка почты",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    return false;
                }

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtpClient.Timeout = 20000;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;
                        mailMessage.To.Add(toEmail);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки Email:\n{ex.Message}",
                    "Ошибка Email", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
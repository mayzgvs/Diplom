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
                var s = Properties.Settings.Default;

                string host = s.SmtpHost?.Trim() ?? "smtp.yandex.ru";
                int port = s.SmtpPort > 0 ? s.SmtpPort : 587;
                bool enableSsl = s.SmtpEnableSsl;
                bool useDefaultCreds = s.SmtpUseDefaultCredentials;

                string authEmail = s.SmtpEmail?.Trim();
                string password = s.SmtpPassword;
                string fromEmail = string.IsNullOrWhiteSpace(s.SmtpFromEmail)
                    ? authEmail
                    : s.SmtpFromEmail.Trim();

                string senderName = s.SmtpSenderName ?? "Автосервис";

                if (string.IsNullOrWhiteSpace(host))
                {
                    MessageBox.Show("SMTP сервер не настроен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                using (var smtpClient = new SmtpClient(host, port))
                {
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.Timeout = 30000;

                    if (useDefaultCreds)
                    {
                        smtpClient.UseDefaultCredentials = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(authEmail) && !string.IsNullOrWhiteSpace(password))
                    {
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(authEmail, password);
                    }

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail, senderName);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;
                        mailMessage.To.Add(toEmail);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return true;
            }
            catch (SmtpException ex)
            {
                MessageBox.Show($"Ошибка SMTP:\n{ex.Message}\nКод: {ex.StatusCode}",
                    "Ошибка отправки", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
using Service.Data;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Service.Services
{
    public class NotificationService
    {
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;

        private const int READY_STATUS_ID = 3;

        public NotificationService()
        {
            _emailService = new EmailService();
            _smsService = new SmsService();
        }

        // Автоматическая отправка при смене статуса
        public async Task SendNotificationOnStatusChange(RepairRequest request, int oldStatusId, int newStatusId)
        {
            if (newStatusId == READY_STATUS_ID && oldStatusId != READY_STATUS_ID)
            {
                await SendReadyNotification(request);
            }
        }

        // Ручная отправка уведомления
        public async Task SendManualNotificationAsync(RepairRequest request, bool sendEmail, bool sendSms)
        {
            var client = request.Car?.Client;
            if (client == null) return;

            var carInfo = $"{request.Car.Brand} {request.Car.Model} ({request.Car.RegistrationNumber})";
            var message = $"Уважаемый(ая) {client.FullName}! Ваш автомобиль {carInfo} готов к выдаче. Ждем Вас в нашем автосервисе.";

            bool emailSent = false;
            bool smsSent = false;

            if (sendEmail && !string.IsNullOrWhiteSpace(client.Email))
            {
                var subject = "Автомобиль готов к выдаче";
                var htmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2 style='color: #3498DB;'>Уважаемый(ая) {client.FullName}!</h2>
                        <p>Ваш автомобиль <strong>{carInfo}</strong> готов к выдаче.</p>
                        <p>Ждем Вас в нашем автосервисе.</p>
                        <br/>
                        <hr/>
                        <p style='color: #7F8C8D; font-size: 12px;'>Это сообщение отправлено автоматически.</p>
                    </body>
                    </html>";

                emailSent = await _emailService.SendEmailAsync(client.Email, subject, htmlBody);
            }

            if (sendSms && !string.IsNullOrWhiteSpace(client.ContactNumber))
            {
                smsSent = await _smsService.SendSmsAsync(client.ContactNumber, message);
            }

            if (emailSent || smsSent)
            {
                MessageBox.Show($"Уведомления отправлены:\n{(emailSent ? "✓ Email\n" : "")}{(smsSent ? "✓ SMS" : "")}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task SendReadyNotification(RepairRequest request)
        {
            await SendManualNotificationAsync(request, true, true);
        }
    }
}
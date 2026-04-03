using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Service.Services
{
    public class SmsService
    {
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return false;

                var cleanedPhone = CleanPhoneNumber(phoneNumber);

                MessageBox.Show($"ТЕСТОВАЯ SMS\n\n" +
                               $"Номер: {cleanedPhone}\n" +
                               $"Сообщение: {message}\n\n" +
                               $"В реальной версии подключите SMS-шлюз (SMS.ru, SMSC и т.д.)",
                    "Тестовая отправка SMS", MessageBoxButton.OK, MessageBoxImage.Information);


                await Task.Delay(300);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка SMS: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private string CleanPhoneNumber(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("8"))
                return "7" + digits.Substring(1);
            if (digits.Length == 10)
                return "7" + digits;

            return digits;
        }
    }
}
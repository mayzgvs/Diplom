using System.Text.RegularExpressions;

namespace Service.Utility
{
    public static class ValidationHelper
    {
        public static bool IsValidRussianLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return false;

            // Регулярное выражение для российского госномера
            // Формат: Б123ББ 777 или Б123ББ777
            var pattern = @"^[АВЕКМНОРСТУХ]\d{3}[АВЕКМНОРСТУХ]{2}\d{2,3}$";
            return Regex.IsMatch(plate.ToUpper(), pattern);
        }

        public static bool IsValidVIN(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin))
                return false;

            // VIN состоит из 17 символов (цифры и латинские буквы, кроме I, O, Q)
            var pattern = @"^[A-HJ-NPR-Z0-9]{17}$";
            return Regex.IsMatch(vin.ToUpper(), pattern);
        }

        public static bool IsValidRussianPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Очищаем номер от лишних символов
            var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Проверяем формат +7XXXXXXXXXX (11 цифр после +7)
            var pattern = @"^\+7\d{10}$";
            return Regex.IsMatch(cleaned, pattern);
        }

        public static string CleanPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;
            return phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        }
    }
}
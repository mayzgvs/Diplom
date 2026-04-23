using System.Text.RegularExpressions;

namespace Service.Utility
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Проверяет корректность российского государственного номера
        /// </summary>
        /// <param name="plate">Номер автомобиля</param>
        /// <returns>true - если номер корректный, false - если нет</returns>
        public static bool IsValidRussianLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return false;

            // Удаляем пробелы и приводим к верхнему регистру
            plate = plate.Trim().ToUpper();

            // Разрешенные буквы для российских номеров
            // А, В, Е, К, М, Н, О, Р, С, Т, У, Х

            // Паттерн для легковых автомобилей: Буква + 3 цифры + 2 буквы + код региона (2-3 цифры)
            string patternCar = @"^[АВЕКМНОРСТУХ]{1}\d{3}[АВЕКМНОРСТУХ]{2}\d{2,3}$";

            // Паттерн для прицепов/транзита: 2 буквы + 3 цифры + код региона
            string patternTrailer = @"^[АВЕКМНОРСТУХ]{2}\d{3}\d{2,3}$";

            // Паттерн для старых номеров: буква + 3 цифры + код региона (без дополнительных букв)
            string patternOld = @"^[АВЕКМНОРСТУХ]{1}\d{3}\d{2,3}$";

            // Паттерн для номеров такси/транзит с двумя буквами в конце
            string patternTaxi = @"^\d{3}[АВЕКМНОРСТУХ]{2}\d{2,3}$";

            return Regex.IsMatch(plate, patternCar) ||
                   Regex.IsMatch(plate, patternTrailer) ||
                   Regex.IsMatch(plate, patternOld) ||
                   Regex.IsMatch(plate, patternTaxi);
        }

        public static bool IsValidVIN(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin))
                return false;

            vin = vin.Trim().ToUpper();

            // VIN должен содержать 17 символов
            if (vin.Length != 17)
                return false;

            // VIN не может содержать буквы I, O, Q
            if (vin.Contains("I") || vin.Contains("O") || vin.Contains("Q"))
                return false;

            // Допустимые символы: 0-9, A-Z
            return Regex.IsMatch(vin, @"^[A-HJ-NPR-Z0-9]{17}$");
        }

        public static bool IsValidRussianPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

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

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Простая проверка формата email
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
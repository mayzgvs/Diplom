using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ClientAddEditModel
    {
        public void CreateClient(string firstName, string lastName, string contactNumber, int discount)
        {
            // Очищаем номер телефона перед сохранением
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.CreateClient(firstName, lastName, cleanedPhone, discount);
        }

        public void EditClient(int id, string firstName, string lastName, string contactNumber, int discount)
        {
            // Очищаем номер телефона перед сохранением
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.EditClient(id, firstName, lastName, cleanedPhone, discount);
        }

        public bool PhoneExists(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Очищаем номер для сравнения
            var cleanedPhone = ValidationHelper.CleanPhone(phone);
            var clients = DbManager.GetClients();
            return clients.Any(client =>
                client.ContactNumber?.Equals(cleanedPhone) == true &&
                (!excludeId.HasValue || client.Id != excludeId.Value));
        }

        // Удаляем методы IsValidPhone из этого класса
        // Они теперь в ValidationHelper
    }
}
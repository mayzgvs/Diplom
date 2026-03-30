using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class EmployeeAddEditModel
    {
        public void CreateEmployee(string firstName, string lastName, string contactNumber)
        {
            // Очищаем номер телефона перед сохранением
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.CreateEmployee(firstName, lastName, cleanedPhone);
        }

        public void EditEmployee(int id, string firstName, string lastName, string contactNumber)
        {
            // Очищаем номер телефона перед сохранением
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.EditEmployee(id, firstName, lastName, cleanedPhone);
        }

        public bool PhoneExists(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Очищаем номер для сравнения
            var cleanedPhone = ValidationHelper.CleanPhone(phone);
            var employees = DbManager.GetEmployees();
            return employees.Any(emp =>
                emp.ContactNumber?.Equals(cleanedPhone) == true &&
                (!excludeId.HasValue || emp.Id != excludeId.Value));
        }

        // Удаляем методы IsValidPhone из этого класса
        // Они теперь в ValidationHelper
    }
}
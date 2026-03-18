using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Service.Models
{
    public class EmployeeAddEditModel
    {
        public void CreateEmployee(string firstName, string lastName, string contactNumber)
        {
            DbManager.CreateEmployee(firstName, lastName, contactNumber);
        }

        public void EditEmployee(int id, string firstName, string lastName, string contactNumber)
        {
            DbManager.EditEmployee(id, firstName, lastName, contactNumber);
        }

        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true;

            var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            return Regex.IsMatch(cleaned, @"^\+?[0-9]{10,15}$");
        }

        public bool PhoneExists(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var employees = DbManager.GetEmployees();
            return employees.Any(emp =>
                emp.ContactNumber?.Equals(phone) == true &&
                (!excludeId.HasValue || emp.Id != excludeId.Value));
        }
    }
}
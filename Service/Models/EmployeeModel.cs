using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class EmployeeModel
    {
        public List<Employee> GetEmployees()
        {
            return DbManager.GetEmployees();
        }

        private List<Employee> _cachedEmployees;

        public List<Employee> GetCachedEmployees()
        {
            if (_cachedEmployees == null)
                _cachedEmployees = DbManager.GetEmployees();
            return _cachedEmployees;
        }

        public void Refresh()
        {
            _cachedEmployees = DbManager.GetEmployees();
        }

        public List<Employee> FilterEmployees(string searchText)
        {
            var employees = GetEmployees();

            if (string.IsNullOrEmpty(searchText))
                return employees;

            searchText = searchText.ToLower();
            return employees.Where(e =>
                (e.LastName?.ToLower().Contains(searchText) == true) ||
                (e.FirstName?.ToLower().Contains(searchText) == true) ||
                (e.ContactNumber?.ToLower().Contains(searchText) == true)
            ).ToList();
        }

        public bool CheckSelectedItem(Employee employee)
        {
            return employee != null;
        }

        public void DeleteEmployee(Employee employee)
        {
            DbManager.DeleteEmployeeById(employee.Id);
            Refresh();
        }

        public bool HasWorkItems(Employee employee)
        {
            return DbManager.GetWorkItemsByEmployeeId(employee.Id).Any();
        }

        public int GetActiveEmployeesCount()
        {
            return DbManager.GetActiveEmployeesCount();
        }
    }
}
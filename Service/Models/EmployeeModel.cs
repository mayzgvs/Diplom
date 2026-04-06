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

        public void Refresh()
        {
            _cachedEmployees = DbManager.GetEmployees();
        }

        public void DeleteEmployee(Employee employee)
        {
            DbManager.DeleteEmployeeById(employee.Id);
            Refresh();
        }

        public int GetActiveEmployeesCount()
        {
            return DbManager.GetActiveEmployeesCount();
        }
    }
}
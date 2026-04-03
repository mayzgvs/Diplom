using Service.Data;
using Service.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class WorkItemAddEditModel
    {
        public List<Data.Service> GetServices() => DbManager.GetServices();
        public List<Consumable> GetConsumables() => DbManager.GetConsumables();
        public List<Employee> GetEmployees() => DbManager.GetEmployees();
        public List<StatusWork> GetWorkStatuses() => DbManager.GetWorkStatuses();

        public void CreateWorkItem(int repairRequestId, int? employeeId, int? serviceId,
            int? consumableId, decimal cost, int statusId)
        {
            DbManager.CreateWorkItem(repairRequestId, employeeId, serviceId, consumableId, cost, statusId);
        }

        public void EditWorkItem(int id, int repairRequestId, int? employeeId, int? serviceId,
            int? consumableId, decimal cost, int statusId)
        {
            DbManager.EditWorkItem(id, repairRequestId, employeeId, serviceId, consumableId, cost, statusId);
        }
    }
}
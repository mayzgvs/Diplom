using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class WorkItemModel
    {
        public List<RepairRequest> GetRepairRequests()
        {
            return DbManager.GetRepairRequests();
        }

        /// <summary>
        /// Получить список всех автомобилей
        /// </summary>
        public List<Car> GetCars()
        {
            return DbManager.GetCars();
        }

        /// <summary>
        /// Получить все заявки по выбранному автомобилю
        /// </summary>
        public List<RepairRequest> GetRepairRequestsByCarId(int carId)
        {
            return DbManager.GetRepairRequestsByCarId(carId);
        }

        public List<WorkItem> GetWorkItemsByRequestId(int requestId)
        {
            return DbManager.GetWorkItemsByRequestId(requestId);
        }

        public List<Employee> GetEmployees()
        {
            return DbManager.GetEmployees();
        }

        public List<Data.Service> GetServices()
        {
            return DbManager.GetServices();
        }

        public List<Consumable> GetConsumables()
        {
            return DbManager.GetConsumables();
        }

        public List<StatusWork> GetWorkStatuses()
        {
            return DbManager.GetWorkStatuses();
        }

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

        public void DeleteWorkItem(WorkItem workItem)
        {
            DbManager.DeleteWorkItemById(workItem.Id);
        }

        public decimal CalculateTotalCost(int requestId)
        {
            return DbManager.GetWorkItemsByRequestId(requestId).Sum(w => w.Cost);
        }

        public void UpdateRequestTotalCost(int requestId, decimal totalCost)
        {
            DbManager.UpdateRepairRequestTotalCost(requestId, totalCost);
        }
    }
}
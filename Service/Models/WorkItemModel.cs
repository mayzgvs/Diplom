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

        public List<Car> GetCars()
        {
            return DbManager.GetCars();
        }

        // Получить все заявки по выбранному автомобилю
        public List<RepairRequest> GetRepairRequestsByCarId(int carId)
        {
            return DbManager.GetRepairRequestsByCarId(carId);
        }

        public List<WorkItem> GetWorkItemsByRequestId(int requestId)
        {
            return DbManager.GetWorkItemsByRequestId(requestId);
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
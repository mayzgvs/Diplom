using Service.Utility;
using Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class RepairRequestAddEditModel
    {
        private List<Car> _cars;
        private List<StatusRequest> _statuses;

        public RepairRequestAddEditModel()
        {
            _cars = DbManager.GetCars();
            _statuses = DbManager.GetRequestStatuses();
        }

        public List<Car> GetCars() => _cars;
        public List<StatusRequest> GetStatuses() => _statuses;

        public void CreateRepairRequest(int carId, DateTime startDate, DateTime? endDate, decimal totalCost, int statusId, int serviceId)
        {
            using (var context = new ApplicationContext())
            {
                var request = new RepairRequest
                {
                    CarId = carId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = totalCost,
                    StatusId = statusId,
                    ServiceId = serviceId
                };
                context.RepairRequests.Add(request);
                context.SaveChanges();
            }
        }

        public void EditRepairRequest(int id, int carId, DateTime startDate, DateTime? endDate, decimal totalCost, int statusId, int serviceId)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(id);
                if (request != null)
                {
                    request.CarId = carId;
                    request.StartDate = startDate;
                    request.EndDate = endDate;
                    request.TotalCost = totalCost;
                    request.StatusId = statusId;
                    request.ServiceId = serviceId;
                    context.SaveChanges();
                }
            }
        }
    }
}
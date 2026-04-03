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

        public string GetClientNameByCarId(int carId)
        {
            var car = _cars.FirstOrDefault(c => c.Id == carId);
            return car?.Client?.FullName ?? string.Empty;  
        }

        public void CreateRepairRequest(int carId, DateTime startDate, DateTime? endDate,
                                        decimal totalCost, int statusId)
        {
            DbManager.CreateRepairRequest(carId, startDate, endDate, totalCost, statusId);
        }

        public void EditRepairRequest(int id, int carId, DateTime startDate, DateTime? endDate,
                                      decimal totalCost, int statusId)
        {
            DbManager.EditRepairRequest(id, carId, startDate, endDate, totalCost, statusId);
        }
    }
}
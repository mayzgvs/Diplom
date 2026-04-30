using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Service.Models
{
    public class CarModel
    {
        public List<Car> GetCars()
        {
            return DbManager.GetCars();
        }

        private List<Car> _cachedCars;

        public void Refresh()
        {
            _cachedCars = DbManager.GetCars();
        }

        public List<RepairRequest> GetRepairRequestsByCarId(int carId)
        {
            return DbManager.GetRepairRequests().Where(r => r.CarId == carId).ToList();
        }

        public void DeleteCar(Car car)
        {
            // Получаем все заявки для этого автомобиля
            var requestsToDelete = DbManager.GetRepairRequests().Where(r => r.CarId == car.Id).ToList();

            foreach (var request in requestsToDelete)
            {
                // Удаляем все работы для каждой заявки
                var workItemsToDelete = DbManager.GetWorkItemsByRequestId(request.Id);
                foreach (var workItem in workItemsToDelete)
                {
                    DbManager.DeleteWorkItemById(workItem.Id);
                }
                // Удаляем заявку
                DbManager.DeleteRepairRequestById(request.Id);
            }
            // Удаляем автомобиль
            DbManager.DeleteCarById(car.Id);
            Refresh();
        }
    }
}
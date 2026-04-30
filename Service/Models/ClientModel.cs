using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Service.Models
{
    public class ClientModel
    {
        public List<Client> GetClients()
        {
            return DbManager.GetClients();
        }

        private List<Client> _cachedClients;

        public void Refresh()
        {
            _cachedClients = DbManager.GetClients();
        }

        public List<Car> GetCarsByClientId(int clientId)
        {
            return DbManager.GetCars().Where(c => c.OwnerId == clientId).ToList();
        }

        public int GetRepairRequestsCountByClientId(int clientId)
        {
            var carIds = DbManager.GetCars().Where(c => c.OwnerId == clientId).Select(c => c.Id).ToList();
            return DbManager.GetRepairRequests().Count(r => carIds.Contains(r.CarId));
        }

        public void DeleteClient(Client client)
        {
            // Получаем все автомобили клиента
            var carsToDelete = DbManager.GetCars().Where(c => c.OwnerId == client.Id).ToList();

            foreach (var car in carsToDelete)
            {
                // Для каждого автомобиля получаем заявки
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
            }
            // Удаляем клиента
            DbManager.DeleteClientById(client.Id);
            Refresh();
        }
    }
}
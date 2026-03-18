using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class CarAddEditModel
    {
        private List<Client> _clients;
        private List<string> _statuses;

        public CarAddEditModel()
        {
            Refresh();
            _statuses = DbManager.GetCarStatuses(); // Получаем статусы
        }

        public void Refresh()
        {
            _clients = DbManager.GetClients();
        }

        public List<string> GetStatuses()
        {
            return _statuses;
        }

        public List<Client> GetClients()
        {
            return _clients;
        }

        public void CreateCar(string brand, string model, string registrationNumber, string vin, int ownerId)
        {
            DbManager.CreateCar(brand, model, registrationNumber, vin, ownerId);
        }

        public void EditCar(int id, string brand, string model, string registrationNumber, string vin, int ownerId)
        {
            DbManager.EditCar(id, brand, model, registrationNumber, vin, ownerId);
        }

        public bool RegistrationNumberExists(string registrationNumber, int? excludeId = null)
        {
            var cars = DbManager.GetCars();
            return cars.Any(car =>
                car.RegistrationNumber?.Equals(registrationNumber, System.StringComparison.OrdinalIgnoreCase) == true &&
                (!excludeId.HasValue || car.Id != excludeId.Value));
        }

        public bool VinExists(string vin, int? excludeId = null)
        {
            var cars = DbManager.GetCars();
            return cars.Any(car =>
                car.VIN?.Equals(vin, System.StringComparison.OrdinalIgnoreCase) == true &&
                (!excludeId.HasValue || car.Id != excludeId.Value));
        }
    }
}
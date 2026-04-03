using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class CarModel
    {
        public List<Car> GetCars()
        {
            return DbManager.GetCars();
        }

        private List<Car> _cachedCars;

        public List<Car> GetCachedCars()
        {
            if (_cachedCars == null)
                _cachedCars = DbManager.GetCars();
            return _cachedCars;
        }

        public void Refresh()
        {
            _cachedCars = DbManager.GetCars();
        }

        public List<Car> FilterCars(string searchText, string status)
        {
            var cars = GetCars();
            var filtered = cars.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                filtered = filtered.Where(c =>
                    c.Brand?.ToLower().Contains(searchText) == true ||
                    c.Model?.ToLower().Contains(searchText) == true ||
                    c.RegistrationNumber?.ToLower().Contains(searchText) == true ||
                    c.VIN?.ToLower().Contains(searchText) == true
                );
            }

            if (!string.IsNullOrEmpty(status) && status == "В ремонте")
            {
                var carsInRepair = DbManager.GetRepairRequests()
                    .Where(r => r.StatusId == 1 || r.StatusId == 2)
                    .Select(r => r.CarId)
                    .Distinct()
                    .ToList();
                filtered = filtered.Where(c => carsInRepair.Contains(c.Id));
            }

            return filtered.ToList();
        }

        public bool CheckSelectedItem(Car car)
        {
            return car != null;
        }

        public void DeleteCar(Car car)
        {
            DbManager.DeleteCarById(car.Id);
            Refresh();
        }

        public bool HasRepairRequests(Car car)
        {
            return DbManager.GetRepairRequestsByCarId(car.Id).Any();
        }
    }
}
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

        public void Refresh()
        {
            _cachedCars = DbManager.GetCars();
        }

        public void DeleteCar(Car car)
        {
            DbManager.DeleteCarById(car.Id);
            Refresh();
        }
    }
}
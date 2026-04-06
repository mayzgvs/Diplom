using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using ServiceModel = Service.Data.Service;

namespace Service.Models
{
    public class ServiceItemModel
    {
        public List<ServiceModel> GetServices()
        {
            return DbManager.GetServices();
        }

        private List<ServiceModel> _cachedServices;
        private List<ServiceCategory> _cachedCategories;

        public void Refresh()
        {
            _cachedServices = DbManager.GetServices();
            _cachedCategories = DbManager.GetServiceCategories();
        }

        public List<ServiceCategory> GetCategories()
        {
            return DbManager.GetServiceCategories();
        }

        public void DeleteService(ServiceModel service)
        {
            DbManager.DeleteServiceById(service.Id);
            Refresh(); 
        }

        public decimal GetAverageCost()
        {
            var services = GetServices();
            return services.Any() ? services.Average(s => s.Cost) : 0;
        }

        public decimal GetMinCost()
        {
            var services = GetServices();
            return services.Any() ? services.Min(s => s.Cost) : 0;
        }

        public decimal GetMaxCost()
        {
            var services = GetServices();
            return services.Any() ? services.Max(s => s.Cost) : 0;
        }
    }
}
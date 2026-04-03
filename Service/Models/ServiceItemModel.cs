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

        public List<ServiceModel> GetCachedServices()
        {
            if (_cachedServices == null)
                _cachedServices = DbManager.GetServices();
            return _cachedServices;
        }

        public List<ServiceCategory> GetCachedCategories()
        {
            if (_cachedCategories == null)
                _cachedCategories = DbManager.GetServiceCategories();
            return _cachedCategories;
        }

        public void Refresh()
        {
            _cachedServices = DbManager.GetServices();
            _cachedCategories = DbManager.GetServiceCategories();
        }

        public List<ServiceCategory> GetCategories()
        {
            return DbManager.GetServiceCategories();
        }

        public List<ServiceModel> FilterServices(string searchText, int? categoryId)
        {
            var services = GetServices(); 
            var filtered = services.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                filtered = filtered.Where(s =>
                    s.Name?.ToLower().Contains(searchText) == true
                );
            }

            if (categoryId.HasValue)
            {
                filtered = filtered.Where(s => s.ServiceCategoryId == categoryId.Value);
            }

            return filtered.ToList();
        }

        public bool CheckSelectedItem(ServiceModel service)
        {
            return service != null;
        }

        public void DeleteService(ServiceModel service)
        {
            DbManager.DeleteServiceById(service.Id);
            Refresh(); 
        }

        public bool IsUsedInWorkItems(ServiceModel service)
        {
            return DbManager.GetWorkItemsByServiceId(service.Id).Any();
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
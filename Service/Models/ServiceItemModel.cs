using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using ServiceModel = Service.Data.Service;

namespace Service.Models
{
    public class ServiceItemModel
    {
        // Метод для получения всегда свежих данных
        public List<ServiceModel> GetServices()
        {
            return DbManager.GetServices();
        }

        // Кэширование для производительности
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

        // Получение категорий (всегда свежие)
        public List<ServiceCategory> GetCategories()
        {
            return DbManager.GetServiceCategories();
        }

        // Фильтрация услуг
        public List<ServiceModel> FilterServices(string searchText, int? categoryId)
        {
            var services = GetServices(); // Всегда свежие данные
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

        // Проверка выбранного элемента
        public bool CheckSelectedItem(ServiceModel service)
        {
            return service != null;
        }

        // Удаление услуги
        public void DeleteService(ServiceModel service)
        {
            DbManager.DeleteServiceById(service.Id);
            Refresh(); // Обновляем кэш после удаления
        }

        // Проверка использования в работах
        public bool IsUsedInWorkItems(ServiceModel service)
        {
            return DbManager.GetWorkItemsByServiceId(service.Id).Any();
        }

        // Статистика
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
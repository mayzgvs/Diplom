using Service.Utility;
using Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class RepairRequestModel
    {
        // Метод для получения всегда свежих данных
        public List<RepairRequest> GetRepairRequests()
        {
            return DbManager.GetRepairRequests();
        }

        // Кэширование для производительности
        private List<RepairRequest> _cachedRequests;
        private List<StatusRequest> _cachedStatuses;

        public List<RepairRequest> GetCachedRequests()
        {
            if (_cachedRequests == null)
                _cachedRequests = DbManager.GetRepairRequests();
            return _cachedRequests;
        }

        public List<StatusRequest> GetCachedStatuses()
        {
            if (_cachedStatuses == null)
                _cachedStatuses = DbManager.GetRequestStatuses();
            return _cachedStatuses;
        }

        public void Refresh()
        {
            _cachedRequests = DbManager.GetRepairRequests();
            _cachedStatuses = DbManager.GetRequestStatuses();
        }

        // Получение статусов (всегда свежие)
        public List<StatusRequest> GetStatuses()
        {
            return DbManager.GetRequestStatuses();
        }

        // Фильтрация заявок
        public List<RepairRequest> FilterRequests(string searchText, int? statusId)
        {
            var requests = GetRepairRequests(); // Всегда свежие данные
            var filtered = requests.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                filtered = filtered.Where(r =>
                    (r.Car != null && (
                        r.Car.Brand?.ToLower().Contains(searchText) == true ||
                        r.Car.Model?.ToLower().Contains(searchText) == true ||
                        r.Car.RegistrationNumber?.ToLower().Contains(searchText) == true)) ||
                    r.Client?.ToLower().Contains(searchText) == true
                );
            }

            if (statusId.HasValue)
            {
                filtered = filtered.Where(r => r.StatusId == statusId.Value);
            }

            return filtered.OrderByDescending(r => r.StartDate).ToList();
        }

        // Проверка выбранного элемента
        public bool CheckSelectedItem(RepairRequest request)
        {
            return request != null;
        }

        // Удаление заявки
        public void DeleteRepairRequest(RepairRequest request)
        {
            DbManager.DeleteRepairRequestById(request.Id);
            Refresh(); // Обновляем кэш после удаления
        }

        // Проверка наличия работ
        public bool HasWorkItems(RepairRequest request)
        {
            return DbManager.GetWorkItemsByRequestId(request.Id).Any();
        }

        // Расчет общей стоимости
        public decimal CalculateTotalCost(int requestId)
        {
            var workItems = DbManager.GetWorkItemsByRequestId(requestId);
            return workItems.Sum(w => w.Cost);
        }
    }
}
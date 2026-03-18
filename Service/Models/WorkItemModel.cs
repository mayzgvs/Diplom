using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class WorkItemModel
    {
        // Метод для получения всегда свежих данных
        public List<WorkItem> GetWorkItems()
        {
            return DbManager.GetWorkItems();
        }

        // Кэширование для производительности
        private List<WorkItem> _cachedWorkItems;
        private List<StatusWork> _cachedStatuses;

        public List<WorkItem> GetCachedWorkItems()
        {
            if (_cachedWorkItems == null)
                _cachedWorkItems = DbManager.GetWorkItems();
            return _cachedWorkItems;
        }

        public List<StatusWork> GetCachedStatuses()
        {
            if (_cachedStatuses == null)
                _cachedStatuses = DbManager.GetWorkStatuses();
            return _cachedStatuses;
        }

        public void Refresh()
        {
            _cachedWorkItems = DbManager.GetWorkItems();
            _cachedStatuses = DbManager.GetWorkStatuses();
        }

        // Получение статусов (всегда свежие)
        public List<StatusWork> GetStatuses()
        {
            return DbManager.GetWorkStatuses();
        }

        // Получение работ по заявке
        public List<WorkItem> GetWorkItemsByRequestId(int requestId)
        {
            var workItems = GetWorkItems(); // Всегда свежие данные
            return workItems.Where(w => w.RepairRequestId == requestId).ToList();
        }

        // Проверка выбранного элемента
        public bool CheckSelectedItem(WorkItem workItem)
        {
            return workItem != null;
        }

        // Удаление работы
        public void DeleteWorkItem(WorkItem workItem)
        {
            DbManager.DeleteWorkItemById(workItem.Id);
            Refresh(); // Обновляем кэш после удаления
        }

        // Расчет общей стоимости по заявке
        public decimal CalculateTotalCostByRequest(int requestId)
        {
            var workItems = GetWorkItemsByRequestId(requestId);
            return workItems.Sum(w => w.Cost);
        }
    }
}
using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ConsumableModel
    {
        public List<Consumable> GetConsumables()
        {
            return DbManager.GetConsumables();
        }

        private List<Consumable> _cachedConsumables;
        private List<ConsumablesCategory> _cachedCategories;

        public List<Consumable> GetCachedConsumables()
        {
            if (_cachedConsumables == null)
                _cachedConsumables = DbManager.GetConsumables();
            return _cachedConsumables;
        }

        public List<ConsumablesCategory> GetCachedCategories()
        {
            if (_cachedCategories == null)
                _cachedCategories = DbManager.GetConsumableCategories();
            return _cachedCategories;
        }

        public void Refresh()
        {
            _cachedConsumables = DbManager.GetConsumables();
            _cachedCategories = DbManager.GetConsumableCategories();
        }

        public List<ConsumablesCategory> GetCategories()
        {
            return DbManager.GetConsumableCategories();
        }

        public List<Consumable> FilterConsumables(string searchText, int? categoryId)
        {
            var consumables = GetConsumables(); 
            var filtered = consumables.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                filtered = filtered.Where(c =>
                    c.Name?.ToLower().Contains(searchText) == true
                );
            }

            if (categoryId.HasValue)
            {
                filtered = filtered.Where(c => c.ConsumableCategoryId == categoryId.Value);
            }

            return filtered.ToList();
        }

        public bool CheckSelectedItem(Consumable consumable)
        {
            return consumable != null;
        }

        public void DeleteConsumable(Consumable consumable)
        {
            DbManager.DeleteConsumableById(consumable.Id);
            Refresh();
        }

        public bool IsUsedInWorkItems(Consumable consumable)
        {
            return DbManager.GetWorkItemsByConsumableId(consumable.Id).Any();
        }
    }
}
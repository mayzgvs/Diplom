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

        public void Refresh()
        {
            _cachedConsumables = DbManager.GetConsumables();
            _cachedCategories = DbManager.GetConsumableCategories();
        }

        public List<ConsumablesCategory> GetCategories()
        {
            return DbManager.GetConsumableCategories();
        }

        public void DeleteConsumable(Consumable consumable)
        {
            DbManager.DeleteConsumableById(consumable.Id);
            Refresh();
        }
    }
}
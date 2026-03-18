using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ConsumableAddEditModel
    {
        private List<ConsumablesCategory> _categories;

        public ConsumableAddEditModel()
        {
            _categories = DbManager.GetConsumableCategories();
        }

        public List<ConsumablesCategory> GetCategories()
        {
            return _categories;
        }

        public void CreateConsumable(string name, int categoryId)
        {
            DbManager.CreateConsumable(name, categoryId);
        }

        public void EditConsumable(int id, string name, int categoryId)
        {
            DbManager.EditConsumable(id, name, categoryId);
        }

        public bool ConsumableNameExistsInCategory(string name, int categoryId, int? excludeId = null)
        {
            var consumables = DbManager.GetConsumables();
            return consumables.Any(c =>
                c.Name?.Equals(name, System.StringComparison.OrdinalIgnoreCase) == true &&
                c.ConsumableCategoryId == categoryId &&
                (!excludeId.HasValue || c.Id != excludeId.Value));
        }
    }
}
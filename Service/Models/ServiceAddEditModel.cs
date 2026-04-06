using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;
using ServiceModel = Service.Data.Service;

namespace Service.Models
{
    public class ServiceAddEditModel
    {
        private List<ServiceCategory> _categories;

        public ServiceAddEditModel()
        {
            _categories = DbManager.GetServiceCategories();
        }

        public List<ServiceCategory> GetCategories()
        {
            return _categories;
        }

        public void CreateService(string name, decimal cost, int categoryId)
        {
            DbManager.CreateService(name, cost, categoryId);
        }

        public void EditService(int id, string name, decimal cost, int categoryId)
        {
            DbManager.EditService(id, name, cost, categoryId);
        }
    }
}
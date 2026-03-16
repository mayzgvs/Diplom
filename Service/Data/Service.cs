using Service.ViewModels;
using Service.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Data
{
    [Table("Service")]
    public class Service : BaseViewModel
    {
        private int _id;
        [Key]
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set
            {
                if (_cost != value)
                {
                    _cost = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _serviceCategoryId;
        [ForeignKey("ServiceCategory")]
        public int ServiceCategoryId
        {
            get => _serviceCategoryId;
            set
            {
                if (_serviceCategoryId != value)
                {
                    _serviceCategoryId = value;
                    OnPropertyChanged();
                }
            }
        }

        private ServiceCategory _serviceCategory;
        public virtual ServiceCategory ServiceCategory
        {
            get => _serviceCategory;
            set
            {
                if (_serviceCategory != value)
                {
                    _serviceCategory = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
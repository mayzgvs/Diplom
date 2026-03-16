using Service.Data;
using Service.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Data
{
    [Table("Consumable")]
    public class Consumable : BaseViewModel
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

        private int _consumableCategoryId;
        [ForeignKey("ConsumableCategory")]
        public int ConsumableCategoryId
        {
            get => _consumableCategoryId;
            set
            {
                if (_consumableCategoryId != value)
                {
                    _consumableCategoryId = value;
                    OnPropertyChanged();
                }
            }
        }

        // Связь с категорией расходников
        private ConsumablesCategory _consumableCategory;
        public virtual ConsumablesCategory ConsumableCategory
        {
            get => _consumableCategory;
            set
            {
                if (_consumableCategory != value)
                {
                    _consumableCategory = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
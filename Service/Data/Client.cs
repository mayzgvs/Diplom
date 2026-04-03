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
    [Table("Client")]
    public class Client : BaseViewModel
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

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _discount;
        public int Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _contactNumber;
        public string ContactNumber
        {
            get => _contactNumber;
            set
            {
                if (_contactNumber != value)
                {
                    _contactNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        private ICollection<Car> _cars;
        public virtual ICollection<Car> Cars
        {
            get => _cars;
            set
            {
                if (_cars != value)
                {
                    _cars = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}".Trim();
    }
}
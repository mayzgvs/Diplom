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
    [Table("Car")]
    public class Car : BaseViewModel
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

        private string _brand;
        public string Brand
        {
            get => _brand;
            set
            {
                if (_brand != value)
                {
                    _brand = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _model;
        public string Model
        {
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _registrationNumber;
        public string RegistrationNumber
        {
            get => _registrationNumber;
            set
            {
                if (_registrationNumber != value)
                {
                    _registrationNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _ownerId;
        public int OwnerId
        {
            get => _ownerId;
            set
            {
                if (_ownerId != value)
                {
                    _ownerId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _vin;
        public string VIN
        {
            get => _vin;
            set
            {
                if (_vin != value)
                {
                    _vin = value;
                    OnPropertyChanged();
                }
            }
        }

        private Client _client;
        [ForeignKey("OwnerId")]
        public virtual Client Client 
        {
            get => _client;
            set
            {
                if (_client != value)
                {
                    _client = value;
                    OnPropertyChanged();
                }
            }
        }
        public string DisplayName => $"{Brand} {Model} ({RegistrationNumber})".Trim();
        public string FullName
        {
            get { return string.Format("{0} {1}", Brand ?? "", Model ?? "").Trim(); }
        }
    }
}
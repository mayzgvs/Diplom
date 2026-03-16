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
    [Table("RepairRequest")]
    public class RepairRequest : BaseViewModel
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

        private int _carId;
        [ForeignKey("Car")]
        public int CarId
        {
            get => _carId;
            set
            {
                if (_carId != value)
                {
                    _carId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _client;
        public string Client
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

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _totalCost;
        public decimal TotalCost
        {
            get => _totalCost;
            set
            {
                if (_totalCost != value)
                {
                    _totalCost = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _statusId;
        [ForeignKey("Status")]
        public int StatusId
        {
            get => _statusId;
            set
            {
                if (_statusId != value)
                {
                    _statusId = value;
                    OnPropertyChanged();
                }
            }
        }

        private Car _car;
        public virtual Car Car
        {
            get => _car;
            set
            {
                if (_car != value)
                {
                    _car = value;
                    OnPropertyChanged();
                }
            }
        }

        private StatusRequest _status;
        public virtual StatusRequest Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
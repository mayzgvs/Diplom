using Service.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private int _carId;
        [ForeignKey(nameof(Car))]
        public int CarId
        {
            get => _carId;
            set { if (_carId != value) { _carId = value; OnPropertyChanged(); } }
        }

        private int _serviceId; // ДОБАВИТЬ ЭТО
        [ForeignKey(nameof(Service))]
        public int ServiceId  // ДОБАВИТЬ ЭТО
        {
            get => _serviceId;
            set { if (_serviceId != value) { _serviceId = value; OnPropertyChanged(); } }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set { if (_startDate != value) { _startDate = value; OnPropertyChanged(); } }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set { if (_endDate != value) { _endDate = value; OnPropertyChanged(); } }
        }

        private decimal _totalCost;
        public decimal TotalCost
        {
            get => _totalCost;
            set { if (_totalCost != value) { _totalCost = value; OnPropertyChanged(); } }
        }

        private int _statusId;
        public int StatusId
        {
            get => _statusId;
            set
            {
                if (_statusId != value)
                {
                    var oldStatusId = _statusId;
                    _statusId = value;
                    OnPropertyChanged();
                }
            }
        }

        private Car _car;
        public virtual Car Car
        {
            get => _car;
            set { if (_car != value) { _car = value; OnPropertyChanged(); } }
        }

        private Service _service;
        public virtual Service Service
        {
            get => _service;
            set { if (_service != value) { _service = value; OnPropertyChanged(); } }
        }

        private StatusRequest _status;
        public virtual StatusRequest Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        [NotMapped]
        public string ServiceName { get; set; }

        [NotMapped]
        public string ClientDisplayName => Car?.Client?.FullName ?? "Клиент не указан";

        [NotMapped]
        public string DisplayName => $"№{Id} - {Car?.Brand} {Car?.Model} ({Car?.RegistrationNumber})";

        [NotMapped]
        public string StatusName => Status?.Name ?? "Неизвестен";
    }
}
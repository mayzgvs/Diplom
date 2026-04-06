using Service.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Media;

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

                    OnStatusChanged(oldStatusId, value);
                }
            }
        }

        private Car _car;
        public virtual Car Car
        {
            get => _car;
            set { if (_car != value) { _car = value; OnPropertyChanged(); } }
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

        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        protected virtual void OnStatusChanged(int oldStatusId, int newStatusId)
        {
            StatusChanged?.Invoke(this, new StatusChangedEventArgs(oldStatusId, newStatusId));
        }
    }
    public class StatusChangedEventArgs : EventArgs
    {
        public int OldStatusId { get; }
        public int NewStatusId { get; }

        public StatusChangedEventArgs(int oldStatusId, int newStatusId)
        {
            OldStatusId = oldStatusId;
            NewStatusId = newStatusId;
        }
    }
}
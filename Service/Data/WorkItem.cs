using Service.Data;
using Service.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("WorkItem")]
    public class WorkItem : BaseViewModel
    {
        private int _id;
        [Key]
        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private int _repairRequestId;
        [ForeignKey("RepairRequest")]
        public int RepairRequestId
        {
            get => _repairRequestId;
            set { if (_repairRequestId != value) { _repairRequestId = value; OnPropertyChanged(); } }
        }

        private int? _employeeId;
        [ForeignKey("Employee")]
        public int? EmployeeId
        {
            get => _employeeId;
            set { if (_employeeId != value) { _employeeId = value; OnPropertyChanged(); } }
        }

        private int? _serviceId;
        [ForeignKey("Service")]
        public int? ServiceId
        {
            get => _serviceId;
            set { if (_serviceId != value) { _serviceId = value; OnPropertyChanged(); } }
        }

        private int? _consumableId;
        [ForeignKey("Consumable")]
        public int? ConsumableId
        {
            get => _consumableId;
            set { if (_consumableId != value) { _consumableId = value; OnPropertyChanged(); } }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set { if (_cost != value) { _cost = value; OnPropertyChanged(); } }
        }

        private int _statusId;
        [ForeignKey("StatusWork")]
        public int StatusId
        {
            get => _statusId;
            set { if (_statusId != value) { _statusId = value; OnPropertyChanged(); } }
        }

        public virtual RepairRequest RepairRequest { get; set; }
        public virtual Employee Employee { get; set; }
        public virtual Service Service { get; set; }
        public virtual Consumable Consumable { get; set; }
        public virtual StatusWork StatusWork { get; set; }


        [NotMapped]
        public string ServiceName => Service?.Name ?? "Услуга не указана";

        [NotMapped]
        public string EmployeeFullName => Employee != null
            ? $"{Employee.LastName} {Employee.FirstName}".Trim()
            : "Не назначен";

        [NotMapped]
        public string EmployeeName => EmployeeFullName;
    }
}
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
    [Table("WorkItem")]
    public class WorkItem : BaseViewModel
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

        private int _repairRequestId;
        [ForeignKey("RepairRequest")]
        public int RepairRequestId
        {
            get => _repairRequestId;
            set
            {
                if (_repairRequestId != value)
                {
                    _repairRequestId = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? _employeeId;
        [ForeignKey("Employee")]
        public int? EmployeeId
        {
            get => _employeeId;
            set
            {
                if (_employeeId != value)
                {
                    _employeeId = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? _serviceId;
        [ForeignKey("Service")]
        public int? ServiceId
        {
            get => _serviceId;
            set
            {
                if (_serviceId != value)
                {
                    _serviceId = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? _consumableId;
        [ForeignKey("Consumable")]
        public int? ConsumableId
        {
            get => _consumableId;
            set
            {
                if (_consumableId != value)
                {
                    _consumableId = value;
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

        private int _statusId;
        [ForeignKey("StatusWork")]
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

        private RepairRequest _repairRequest;
        public virtual RepairRequest RepairRequest
        {
            get => _repairRequest;
            set
            {
                if (_repairRequest != value)
                {
                    _repairRequest = value;
                    OnPropertyChanged();
                }
            }
        }

        private Employee _employee;
        public virtual Employee Employee
        {
            get => _employee;
            set
            {
                if (_employee != value)
                {
                    _employee = value;
                    OnPropertyChanged();
                }
            }
        }

        private Service _service;
        public virtual Service Service
        {
            get => _service;
            set
            {
                if (_service != value)
                {
                    _service = value;
                    OnPropertyChanged();
                }
            }
        }

        private Consumable _consumable;
        public virtual Consumable Consumable
        {
            get => _consumable;
            set
            {
                if (_consumable != value)
                {
                    _consumable = value;
                    OnPropertyChanged();
                }
            }
        }

        private StatusWork _statusWork;
        public virtual StatusWork StatusWork
        {
            get => _statusWork;
            set
            {
                if (_statusWork != value)
                {
                    _statusWork = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotMapped]
        public string EmployeeFullName => Employee?.FullName ?? "Не назначен";

        [NotMapped]
        public string ConsumableName => Consumable?.Name ?? "Не указан";

        [NotMapped]
        public string StatusName => StatusWork?.Name ?? "Не указан";

        [NotMapped]
        public string ServiceAndConsumableName
        {
            get
            {
                string result = "";
                if (Service != null)
                    result += Service.Name;
                if (Consumable != null)
                {
                    if (!string.IsNullOrEmpty(result))
                        result += " + ";
                    result += Consumable.Name;
                }
                return string.IsNullOrEmpty(result) ? "Не указано" : result;
            }
        }

        [NotMapped]
        public string EmployeeName
        {
            get
            {
                return Employee != null ? $"{Employee.LastName} {Employee.FirstName}" : "Не назначен";
            }
        }

        [NotMapped]
        public string ServiceName
        {
            get
            {
                return Service != null ? Service.Name : "Услуга не указана";
            }
        }
        public int Index { get; set; } 
    }
}
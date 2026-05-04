using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddWorkItemViewModel : BaseViewModel
    {
        private readonly WorkItemAddEditModel _model = new WorkItemAddEditModel();
        private readonly RepairRequest _repairRequest;
        private readonly WorkItem _editingWorkItem;

        public event EventHandler WorkItemSaved;

        public string Title => _editingWorkItem == null ? "Добавление работы" : "Редактирование работы";

        public string RepairRequestInfo => _repairRequest != null
            ? $"{_repairRequest.Car?.Brand} {_repairRequest.Car?.Model} ({_repairRequest.Car?.RegistrationNumber})"
            : "";

        public string ClientInfo => _repairRequest?.Car?.Client?.FullName ?? "Не указан";

        public ObservableCollection<Data.Service> Services { get; private set; }
        public ObservableCollection<Consumable> Consumables { get; private set; }
        public ObservableCollection<Employee> Employees { get; private set; }
        public ObservableCollection<StatusWork> WorkStatuses { get; private set; }

        private bool _isServiceSelected = true;
        public bool IsServiceSelected
        {
            get => _isServiceSelected;
            set
            {
                _isServiceSelected = value;
                OnPropertyChanged();
                CalculateTotalCost();
            }
        }

        private bool _isConsumableSelected;
        public bool IsConsumableSelected
        {
            get => _isConsumableSelected;
            set
            {
                _isConsumableSelected = value;
                OnPropertyChanged();
                CalculateTotalCost();
            }
        }

        private Data.Service _selectedService;
        public Data.Service SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
                CalculateTotalCost();
            }
        }

        private Consumable _selectedConsumable;
        public Consumable SelectedConsumable
        {
            get => _selectedConsumable;
            set
            {
                _selectedConsumable = value;
                OnPropertyChanged();
                CalculateTotalCost();
            }
        }

        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
            }
        }

        private StatusWork _selectedStatus;
        public StatusWork SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
            }
        }

        private string _displayCost = "0 ₽";
        public string DisplayCost
        {
            get => _displayCost;
            private set
            {
                _displayCost = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveCommand { get; }

        public AddWorkItemViewModel(RepairRequest repairRequest, WorkItem editingWorkItem = null)
        {
            _repairRequest = repairRequest ?? throw new ArgumentNullException(nameof(repairRequest));
            _editingWorkItem = editingWorkItem;

            LoadData();
            InitializeValues();

            SaveCommand = new RelayCommand(Save);
        }

        private void LoadData()
        {
            Services = new ObservableCollection<Data.Service>(_model.GetServices().OrderBy(s => s.Name));
            Consumables = new ObservableCollection<Consumable>(_model.GetConsumables().OrderBy(c => c.Name));
            Employees = new ObservableCollection<Employee>(_model.GetEmployees().OrderBy(e => e.LastName).ThenBy(e => e.FirstName));
            WorkStatuses = new ObservableCollection<StatusWork>(_model.GetWorkStatuses().OrderBy(s => s.Name));
        }

        private void InitializeValues()
        {
            if (_editingWorkItem != null)
            {
                IsServiceSelected = _editingWorkItem.ServiceId.HasValue;
                IsConsumableSelected = _editingWorkItem.ConsumableId.HasValue;

                SelectedService = Services.FirstOrDefault(s => s.Id == _editingWorkItem.ServiceId);
                SelectedConsumable = Consumables.FirstOrDefault(c => c.Id == _editingWorkItem.ConsumableId);
                SelectedEmployee = Employees.FirstOrDefault(e => e.Id == _editingWorkItem.EmployeeId);
                SelectedStatus = WorkStatuses.FirstOrDefault(s => s.Id == _editingWorkItem.StatusId);

                CalculateTotalCost();
            }
            else
            {
                SelectedStatus = WorkStatuses.FirstOrDefault();
                IsServiceSelected = true;
                IsConsumableSelected = false;
                CalculateTotalCost();
            }
        }

        private void CalculateTotalCost()
        {
            decimal total = 0;

            if (IsServiceSelected && SelectedService != null)
                total += SelectedService.Cost;

            if (IsConsumableSelected && SelectedConsumable != null)
                total += SelectedConsumable.Cost ?? 0;

            DisplayCost = total > 0 ? $"{total:N0} ₽" : "0 ₽";
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";

            if (SelectedEmployee == null)
            {
                ErrorMessage = "Выберите сотрудника!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedStatus == null)
            {
                ErrorMessage = "Выберите статус работы!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsServiceSelected && !IsConsumableSelected)
            {
                ErrorMessage = "Выберите услугу или расходный материал!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsServiceSelected && SelectedService == null)
            {
                ErrorMessage = "Выберите услугу!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsConsumableSelected && SelectedConsumable == null)
            {
                ErrorMessage = "Выберите расходный материал!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal costValue = 0;

            if (IsServiceSelected && SelectedService != null)
                costValue += SelectedService.Cost;

            if (IsConsumableSelected && SelectedConsumable != null)
                costValue += SelectedConsumable.Cost ?? 0;

            if (costValue == 0)
            {
                var result = CustomMessageBox.Show("Стоимость работы равна 0. Продолжить?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                int? serviceId = IsServiceSelected ? SelectedService?.Id : null;
                int? consumableId = IsConsumableSelected ? SelectedConsumable?.Id : null;

                if (_editingWorkItem == null)
                {
                    _model.CreateWorkItem(_repairRequest.Id, SelectedEmployee?.Id, serviceId, consumableId,
                                        costValue, SelectedStatus.Id);
                    CustomMessageBox.Show("Работа успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditWorkItem(_editingWorkItem.Id, _repairRequest.Id, SelectedEmployee?.Id,
                                      serviceId, consumableId, costValue, SelectedStatus.Id);
                    CustomMessageBox.Show("Работа успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                WorkItemSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                CustomMessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
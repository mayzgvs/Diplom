using Service.Data;
using Service.Models;
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

        public string RepairRequestInfo => _repairRequest != null ?
            $"{_repairRequest.Car?.Brand} {_repairRequest.Car?.Model} ({_repairRequest.Car?.RegistrationNumber})" : "";

        public string ClientInfo => _repairRequest?.Car?.Client?.FullName ?? "";

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
                if (!value) SelectedService = null;
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
                if (!value) SelectedConsumable = null;
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
            set { _selectedEmployee = value; OnPropertyChanged(); }
        }

        private StatusWork _selectedStatus;
        public StatusWork SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        private string _cost = "0";
        public string Cost
        {
            get => _cost;
            set
            {
                _cost = value;
                OnPropertyChanged();
            }
        }

        // Отображаемая стоимость (с форматированием)
        private string _displayCost;
        public string DisplayCost
        {
            get => _displayCost;
            set { _displayCost = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public ICommand SaveCommand { get; }

        public AddWorkItemViewModel(RepairRequest repairRequest, WorkItem editingWorkItem = null)
        {
            _repairRequest = repairRequest ?? throw new ArgumentNullException(nameof(repairRequest));
            _editingWorkItem = editingWorkItem;

            LoadData();
            InitializeValues();

            SaveCommand = new RelayCommand(Save, CanSave);
        }

        private void LoadData()
        {
            Services = new ObservableCollection<Data.Service>(_model.GetServices());
            Consumables = new ObservableCollection<Consumable>(_model.GetConsumables());
            Employees = new ObservableCollection<Employee>(_model.GetEmployees());
            WorkStatuses = new ObservableCollection<StatusWork>(_model.GetWorkStatuses());

            OnPropertyChanged(nameof(Services));
            OnPropertyChanged(nameof(Consumables));
            OnPropertyChanged(nameof(Employees));
            OnPropertyChanged(nameof(WorkStatuses));
        }

        private void InitializeValues()
        {
            if (_editingWorkItem != null)
            {
                IsServiceSelected = _editingWorkItem.ServiceId.HasValue;
                IsConsumableSelected = _editingWorkItem.ConsumableId.HasValue;

                if (IsServiceSelected)
                    SelectedService = Services.FirstOrDefault(s => s.Id == _editingWorkItem.ServiceId);

                if (IsConsumableSelected)
                    SelectedConsumable = Consumables.FirstOrDefault(c => c.Id == _editingWorkItem.ConsumableId);

                SelectedEmployee = Employees.FirstOrDefault(e => e.Id == _editingWorkItem.EmployeeId);
                SelectedStatus = WorkStatuses.FirstOrDefault(s => s.Id == _editingWorkItem.StatusId);

                // Устанавливаем стоимость из редактируемой работы
                decimal costValue = _editingWorkItem.Cost;
                _cost = costValue.ToString();
                UpdateDisplayCost(costValue);
            }
            else
            {
                SelectedStatus = WorkStatuses.FirstOrDefault();
                IsServiceSelected = true;
                IsConsumableSelected = false;
                _cost = "0";
                UpdateDisplayCost(0);
            }
        }

        /// <summary>
        /// Автоматический расчет общей стоимости
        /// </summary>
        private void CalculateTotalCost()
        {
            decimal total = 0;

            // Добавляем стоимость услуги, если выбрана
            if (IsServiceSelected && SelectedService != null)
            {
                total += SelectedService.Cost;
            }

            // Добавляем стоимость расходника, если выбран
            if (IsConsumableSelected && SelectedConsumable != null)
            {
                total += SelectedConsumable.Cost ?? 0;
            }

            // Сохраняем числовое значение
            _cost = total.ToString();
            OnPropertyChanged(nameof(Cost));

            // Обновляем отображаемое значение с форматированием
            UpdateDisplayCost(total);
        }

        /// <summary>
        /// Обновление отображаемой стоимости с форматированием
        /// </summary>
        private void UpdateDisplayCost(decimal cost)
        {
            if (cost > 0)
                DisplayCost = $"{cost:N0} ₽";
            else
                DisplayCost = "0 ₽";
        }

        private bool CanSave(object parameter)
        {
            // Проверяем, что выбран хотя бы один тип работы
            bool hasWorkType = IsServiceSelected || IsConsumableSelected;

            // Если выбрана услуга, она должна быть выбрана
            if (IsServiceSelected && SelectedService == null)
                return false;

            // Если выбран расходник, он должен быть выбран
            if (IsConsumableSelected && SelectedConsumable == null)
                return false;

            // Проверяем сотрудника и статус
            return hasWorkType && SelectedEmployee != null && SelectedStatus != null;
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (_repairRequest == null)
            {
                ErrorMessage = "Сначала выберите заявку!";
                return;
            }

            if (!CanSave(parameter))
            {
                if (!IsServiceSelected && !IsConsumableSelected)
                    ErrorMessage = "Выберите тип работы (услуга или расходный материал)!";
                else if (SelectedEmployee == null)
                    ErrorMessage = "Выберите сотрудника!";
                else if (SelectedStatus == null)
                    ErrorMessage = "Выберите статус!";
                else
                    ErrorMessage = "Заполните все обязательные поля!";
                return;
            }

            if (!decimal.TryParse(_cost, out decimal costValue) || costValue <= 0)
            {
                // Если стоимость 0, но выбраны услуга или расходник - возможно они бесплатные
                if (costValue == 0 && (IsServiceSelected || IsConsumableSelected))
                {
                    // Разрешаем сохранение с нулевой стоимостью
                    costValue = 0;
                }
                else
                {
                    ErrorMessage = "Укажите корректную стоимость!";
                    return;
                }
            }

            try
            {
                int? serviceId = IsServiceSelected ? SelectedService?.Id : null;
                int? consumableId = IsConsumableSelected ? SelectedConsumable?.Id : null;

                if (_editingWorkItem == null)
                {
                    _model.CreateWorkItem(_repairRequest.Id, SelectedEmployee?.Id,
                        serviceId, consumableId, costValue, SelectedStatus.Id);
                    SuccessMessage = "Работа успешно добавлена!";
                }
                else
                {
                    _model.EditWorkItem(_editingWorkItem.Id, _repairRequest.Id, SelectedEmployee?.Id,
                        serviceId, consumableId, costValue, SelectedStatus.Id);
                    SuccessMessage = "Работа успешно обновлена!";
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
            }
        }
    }
}
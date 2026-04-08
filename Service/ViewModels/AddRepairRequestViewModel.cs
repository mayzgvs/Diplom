using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddRepairRequestViewModel : BaseViewModel
    {
        private readonly RepairRequestAddEditModel _model = new RepairRequestAddEditModel();
        private readonly ApplicationContext _context = new ApplicationContext();

        private RepairRequest _editingRepairRequest;
        private bool _isEditMode;

        public RepairRequest EditingRepairRequest
        {
            get => _editingRepairRequest;
            set { _editingRepairRequest = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Car> Cars { get; private set; }
        public ObservableCollection<StatusRequest> Statuses { get; private set; }
        public ObservableCollection<Data.Service> Services { get; private set; }

        private string _clientName;
        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        private Data.Service _selectedService;
        public Data.Service SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
                if (value != null)
                    EditingRepairRequest.TotalCost = value.Cost;
            }
        }

        // Свойство для отображения ошибки дат
        private string _dateError;
        public string DateError
        {
            get => _dateError;
            set { _dateError = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewCarCommand { get; }

        public AddRepairRequestViewModel(RepairRequest request = null)
        {
            Cars = new ObservableCollection<Car>(_model.GetCars());
            Statuses = new ObservableCollection<StatusRequest>(_model.GetStatuses());
            Services = new ObservableCollection<Data.Service>(GetServices());

            if (request == null)
            {
                _isEditMode = false;
                EditingRepairRequest = new RepairRequest
                {
                    StartDate = DateTime.Now.Date,
                    EndDate = DateTime.Now.Date.AddDays(1)
                };
            }
            else
            {
                _isEditMode = true;
                EditingRepairRequest = request;
                ClientName = EditingRepairRequest.ClientDisplayName;
                SelectedService = Services.FirstOrDefault(s => s.Name == EditingRepairRequest.ServiceName);
            }

            EditingRepairRequest.PropertyChanged += EditingRepairRequest_PropertyChanged;

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewCarCommand = new RelayCommand(AddNewCar);
        }

        private List<Data.Service> GetServices()
        {
            using (var context = new ApplicationContext())
                return context.Services.ToList();
        }

        private string GetClientNameByCarId(int carId)
        {
            var car = _context.Cars.FirstOrDefault(c => c.Id == carId);
            if (car != null)
            {
                var client = _context.Clients.FirstOrDefault(c => c.Id == car.OwnerId);
                if (client != null)
                {
                    return $"{client.LastName} {client.FirstName}".Trim();
                }
            }
            return "Не указан";
        }

        private void EditingRepairRequest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RepairRequest.CarId))
            {
                ClientName = GetClientNameByCarId(EditingRepairRequest.CarId);
            }

            // Проверка дат при любом изменении StartDate или EndDate
            if (e.PropertyName == nameof(RepairRequest.StartDate) ||
                e.PropertyName == nameof(RepairRequest.EndDate))
            {
                ValidateDates();
            }
        }

        private void ValidateDates()
        {
            if (EditingRepairRequest.StartDate > EditingRepairRequest.EndDate)
                DateError = "Дата окончания не может быть раньше даты начала!";
            else
                DateError = null;
        }

        private void Save(object parameter)
        {
            ValidateDates();

            if (!string.IsNullOrEmpty(DateError))
            {
                MessageBox.Show("Ошибка: дата окончания не может быть раньше даты начала!",
                    "Ошибка дат", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.CarId == 0)
            {
                MessageBox.Show("Выберите автомобиль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedService == null)
            {
                MessageBox.Show("Выберите услугу!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingRepairRequest.ServiceName = SelectedService.Name;

            if (!_isEditMode)
                _model.CreateRepairRequest(EditingRepairRequest.CarId, EditingRepairRequest.StartDate,
                    EditingRepairRequest.EndDate, EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);
            else
                _model.EditRepairRequest(EditingRepairRequest.Id, EditingRepairRequest.CarId,
                    EditingRepairRequest.StartDate, EditingRepairRequest.EndDate,
                    EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);

            if (parameter is Window window)
                window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window) window.DialogResult = false;
        }

        private void AddNewCar(object parameter)
        {
            MessageBox.Show("Создание авто из формы заявки будет добавлено позже", "Инфо");
        }
    }
}
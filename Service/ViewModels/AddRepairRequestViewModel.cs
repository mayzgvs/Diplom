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

        private RepairRequest _editingRepairRequest;
        private bool _isEditMode;

        public event EventHandler RepairRequestSaved;

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
                if (value != null && EditingRepairRequest != null)
                {
                    EditingRepairRequest.TotalCost = value.Cost;
                    EditingRepairRequest.ServiceName = value.Name;
                    EditingRepairRequest.ServiceId = value.Id;
                }
            }
        }

        private string _dateError;
        public string DateError
        {
            get => _dateError;
            set { _dateError = value; OnPropertyChanged(); }
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

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                _successMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSuccess));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewCarCommand { get; }

        public AddRepairRequestViewModel(RepairRequest request = null)
        {
            LoadData();

            if (request == null)
            {
                _isEditMode = false;
                EditingRepairRequest = new RepairRequest
                {
                    StartDate = DateTime.Now.Date,
                    EndDate = DateTime.Now.Date.AddDays(1),
                    StatusId = 1,
                    ServiceId = 0
                };
            }
            else
            {
                _isEditMode = true;
                EditingRepairRequest = request;

                ClientName = EditingRepairRequest.ClientDisplayName;

                if (EditingRepairRequest.ServiceId > 0)
                {
                    SelectedService = Services.FirstOrDefault(s => s.Id == EditingRepairRequest.ServiceId);
                }
            }

            EditingRepairRequest.PropertyChanged += EditingRepairRequest_PropertyChanged;

            SaveCommand = new RelayCommand(Save);       
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewCarCommand = new RelayCommand(AddNewCar);
        }

        private void LoadData()
        {
            Cars = new ObservableCollection<Car>(_model.GetCars());
            Statuses = new ObservableCollection<StatusRequest>(_model.GetStatuses());
            Services = new ObservableCollection<Data.Service>(GetServices());

            OnPropertyChanged(nameof(Cars));
            OnPropertyChanged(nameof(Statuses));
            OnPropertyChanged(nameof(Services));
        }

        private List<Data.Service> GetServices()
        {
            using (var context = new ApplicationContext())
                return context.Services.ToList();
        }

        private string GetClientNameByCarId(int carId)
        {
            if (carId == 0) return "Не указан";

            using (var context = new ApplicationContext())
            {
                var car = context.Cars.FirstOrDefault(c => c.Id == carId);
                if (car?.OwnerId != null)
                {
                    var client = context.Clients.FirstOrDefault(c => c.Id == car.OwnerId);
                    if (client != null)
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

            if (e.PropertyName == nameof(RepairRequest.StartDate) ||
                e.PropertyName == nameof(RepairRequest.EndDate))
            {
                ValidateDates();
            }
        }

        private void ValidateDates()
        {
            DateError = EditingRepairRequest.StartDate > EditingRepairRequest.EndDate
                ? "Дата окончания не может быть раньше даты начала!"
                : null;
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            ValidateDates();

            if (!string.IsNullOrEmpty(DateError))
            {
                ErrorMessage = DateError;
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.CarId == 0)
            {
                ErrorMessage = "Выберите автомобиль!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedService == null)
            {
                ErrorMessage = "Выберите услугу!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateRepairRequest(
                        EditingRepairRequest.CarId,
                        EditingRepairRequest.StartDate,
                        EditingRepairRequest.EndDate,
                        EditingRepairRequest.TotalCost,
                        EditingRepairRequest.StatusId,
                        SelectedService.Id);

                    SuccessMessage = "Заявка успешно добавлена!";
                    MessageBox.Show("Заявка успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditRepairRequest(
                        EditingRepairRequest.Id,
                        EditingRepairRequest.CarId,
                        EditingRepairRequest.StartDate,
                        EditingRepairRequest.EndDate,
                        EditingRepairRequest.TotalCost,
                        EditingRepairRequest.StatusId,
                        SelectedService.Id);

                    SuccessMessage = "Заявка успешно обновлена!";
                    MessageBox.Show("Заявка успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RepairRequestSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }

        private void AddNewCar(object parameter)
        {
            var addCarView = new AddCarView();
            addCarView.ShowDialog();

            Cars.Clear();
            foreach (var car in _model.GetCars())
                Cars.Add(car);
        }
    }
}
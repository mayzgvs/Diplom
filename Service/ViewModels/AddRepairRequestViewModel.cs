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
                {
                    EditingRepairRequest.TotalCost = value.Cost;
                    EditingRepairRequest.ServiceName = value.Name;
                }
            }
        }

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
                    EndDate = DateTime.Now.Date.AddDays(1),
                    StatusId = 1 
                };
                ClientName = string.Empty;
                SelectedService = null;
            }
            else
            {
                _isEditMode = true;
                EditingRepairRequest = request;
                ClientName = EditingRepairRequest.ClientDisplayName;

                LoadSelectedService();
            }

            EditingRepairRequest.PropertyChanged += EditingRepairRequest_PropertyChanged;

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewCarCommand = new RelayCommand(AddNewCar);
        }

        private void LoadSelectedService()
        {
            if (!string.IsNullOrEmpty(EditingRepairRequest.ServiceName))
            {
                SelectedService = Services.FirstOrDefault(s => s.Name == EditingRepairRequest.ServiceName);
            }

            if (SelectedService == null && EditingRepairRequest.TotalCost > 0)
            {
                SelectedService = Services.FirstOrDefault(s => s.Cost == EditingRepairRequest.TotalCost);
            }
        }

        private List<Data.Service> GetServices()
        {
            using (var context = new ApplicationContext())
                return context.Services.ToList();
        }

        private void EditingRepairRequest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RepairRequest.CarId))
            {
                ClientName = _model.GetClientNameByCarId(EditingRepairRequest.CarId);
            }

            if (e.PropertyName == nameof(RepairRequest.StartDate) ||
                e.PropertyName == nameof(RepairRequest.EndDate))
            {
                ValidateDates();
            }
        }

        private void ValidateDates()
        {
            if (EditingRepairRequest.EndDate.HasValue &&
                EditingRepairRequest.EndDate.Value.Date < EditingRepairRequest.StartDate.Date)
            {
                DateError = "Дата окончания не может быть раньше даты начала!";
            }
            else if (EditingRepairRequest.StartDate.Date > (EditingRepairRequest.EndDate?.Date ?? DateTime.MaxValue.Date))
            {
                DateError = "Дата начала не может быть позже даты окончания!";
            }
            else
            {
                DateError = string.Empty;
            }
        }

        private void Save(object parameter)
        {
            ValidateDates();

            if (!string.IsNullOrEmpty(DateError))
            {
                MessageBox.Show(DateError, "Ошибка дат", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.CarId == 0)
            {
                MessageBox.Show("Выберите автомобиль!", "Ошибка");
                return;
            }

            if (SelectedService == null)
            {
                MessageBox.Show("Выберите услугу!", "Ошибка");
                return;
            }

            EditingRepairRequest.ServiceName = SelectedService.Name;

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateRepairRequest(EditingRepairRequest.CarId, EditingRepairRequest.StartDate,
                        EditingRepairRequest.EndDate, EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);
                }
                else
                {
                    _model.EditRepairRequest(EditingRepairRequest.Id, EditingRepairRequest.CarId,
                        EditingRepairRequest.StartDate, EditingRepairRequest.EndDate,
                        EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);
                }

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window) window.DialogResult = false;
        }

        private void AddNewCar(object parameter)
        {
            var addCarWindow = new AddCarView();
            var addCarViewModel = new AddCarViewModel();
            addCarWindow.DataContext = addCarViewModel;

            addCarViewModel.CarSaved += (s, e) =>
            {
                Cars.Clear();
                foreach (var car in _model.GetCars())
                {
                    Cars.Add(car);
                }
            };

            addCarWindow.ShowDialog();
        }
    }
}
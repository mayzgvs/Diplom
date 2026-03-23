using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
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

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewCarCommand { get; }

        public AddRepairRequestViewModel(RepairRequest request = null)
        {
            Cars = new ObservableCollection<Car>(_model.GetCars());
            Statuses = new ObservableCollection<StatusRequest>(_model.GetStatuses());

            if (request == null)
            {
                _isEditMode = false;
                EditingRepairRequest = new RepairRequest { StartDate = DateTime.Now };
            }
            else
            {
                _isEditMode = true;
                EditingRepairRequest = request;
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewCarCommand = new RelayCommand(AddNewCar);
        }

        private void AddNewCar(object parameter)
        {
            // Можно добавить вызов окна создания авто
            MessageBox.Show("Создание авто из формы заявки будет добавлено позже", "Инфо");
        }

        private void Save(object parameter)
        {
            if (EditingRepairRequest.CarId == 0 || string.IsNullOrWhiteSpace(EditingRepairRequest.Client))
            {
                MessageBox.Show("Выберите автомобиль и укажите клиента!", "Ошибка");
                return;
            }

            if (!_isEditMode)
                _model.CreateRepairRequest(EditingRepairRequest.CarId, EditingRepairRequest.Client,
                    EditingRepairRequest.StartDate, EditingRepairRequest.EndDate,
                    EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);
            else
                _model.EditRepairRequest(EditingRepairRequest.Id, EditingRepairRequest.CarId, EditingRepairRequest.Client,
                    EditingRepairRequest.StartDate, EditingRepairRequest.EndDate,
                    EditingRepairRequest.TotalCost, EditingRepairRequest.StatusId);

            if (parameter is Window window)
                window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window) window.DialogResult = false;
        }
    }
}
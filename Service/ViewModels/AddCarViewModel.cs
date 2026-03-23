using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddCarViewModel : BaseViewModel
    {
        private readonly CarAddEditModel _model = new CarAddEditModel();
        private Car _editingCar;
        private bool _isEditMode;

        public Car EditingCar
        {
            get => _editingCar;
            set { _editingCar = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Client> Clients { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewClientCommand { get; }

        public AddCarViewModel(Car car = null)
        {
            Clients = new ObservableCollection<Client>(_model.GetClients());

            if (car == null)
            {
                _isEditMode = false;
                EditingCar = new Car();
            }
            else
            {
                _isEditMode = true;
                EditingCar = car;
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewClientCommand = new RelayCommand(AddNewClient);
        }

        private void AddNewClient(object parameter)
        {
            var addClientWindow = new AddClient();
            var addClientViewModel = new AddClientViewModel();
            addClientWindow.DataContext = addClientViewModel;

            if (addClientWindow.ShowDialog() == true)
            {
                // Обновляем список клиентов
                Clients = new ObservableCollection<Client>(_model.GetClients());
            }
        }

        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingCar.Brand) ||
                string.IsNullOrWhiteSpace(EditingCar.Model) ||
                string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber) ||
                EditingCar.OwnerId == 0)
            {
                MessageBox.Show("Заполните все обязательные поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isEditMode)
            {
                _model.CreateCar(EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber, EditingCar.VIN, EditingCar.OwnerId);
            }
            else
            {
                _model.EditCar(EditingCar.Id, EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber, EditingCar.VIN, EditingCar.OwnerId);
            }

            if (parameter is Window window)
                window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
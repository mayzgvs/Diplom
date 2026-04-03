using Service.Data;
using Service.Models;
using Service.Utility;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddCarViewModel : BaseViewModel
    {
        private readonly CarAddEditModel _model = new CarAddEditModel();
        private Car _editingCar;
        private bool _isEditMode;

        public event EventHandler CarSaved;

        public Car EditingCar
        {
            get => _editingCar;
            set { _editingCar = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Client> Clients { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewClientCommand { get; }


        private void LoadClients()
        {
            Clients = new ObservableCollection<Client>(_model.GetClients());
        }

        private void AddNewClient(object parameter)
        {
            var addClientWindow = new AddClient();
            var addClientViewModel = new AddClientViewModel();
            addClientWindow.DataContext = addClientViewModel;

            addClientViewModel.ClientSaved += OnClientSaved;

            if (addClientWindow.ShowDialog() == true)
            {
                LoadClients();
            }

            addClientViewModel.ClientSaved -= OnClientSaved;
        }

        private void OnClientSaved(object sender, EventArgs e)
        {
            LoadClients();
        }

        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingCar.Brand))
            {
                MessageBox.Show("Введите марку автомобиля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingCar.Model))
            {
                MessageBox.Show("Введите модель автомобиля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
            {
                MessageBox.Show("Введите государственный номер!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingCar.OwnerId == 0)
            {
                MessageBox.Show("Выберите владельца автомобиля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidationHelper.IsValidRussianLicensePlate(EditingCar.RegistrationNumber))
            {
                MessageBox.Show("Некорректный формат государственного номера!\nФормат: Б123ББ77 или Б123ББ777",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingCar.VIN))
            {
                if (!ValidationHelper.IsValidVIN(EditingCar.VIN))
                {
                    MessageBox.Show("Некорректный формат VIN номера!\nVIN должен состоять из 17 символов (цифры и латинские буквы, кроме I, O, Q)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.VinExists(EditingCar.VIN, _isEditMode ? EditingCar.Id : (int?)null))
                {
                    MessageBox.Show("Автомобиль с таким VIN номером уже существует в базе!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (_model.RegistrationNumberExists(EditingCar.RegistrationNumber, _isEditMode ? EditingCar.Id : (int?)null))
            {
                MessageBox.Show("Автомобиль с таким государственным номером уже существует в базе!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateCar(EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber,
                        EditingCar.VIN, EditingCar.OwnerId);
                }
                else
                {
                    _model.EditCar(EditingCar.Id, EditingCar.Brand, EditingCar.Model,
                        EditingCar.RegistrationNumber, EditingCar.VIN, EditingCar.OwnerId);
                }

                CarSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public AddCarViewModel(Car car = null)
        {
            LoadClients();

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

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
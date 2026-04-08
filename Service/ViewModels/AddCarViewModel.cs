using Service.Data;
using Service.Models;
using Service.Utility;
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

        private async void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingCar.Brand) || string.IsNullOrWhiteSpace(EditingCar.Model))
            {
                ErrorMessage = "Заполните обязательные поля!";
                return;
            }

            if (EditingCar.OwnerId == 0)
            {
                ErrorMessage = "Выберите владельца автомобиля!";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
            {
                ErrorMessage = "Введите государственный номер!";
                return;
            }

            if (!ValidationHelper.IsValidRussianLicensePlate(EditingCar.RegistrationNumber))
            {
                ErrorMessage = "Ошибка ввода: некорректный формат госномера!";
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingCar.VIN))
            {
                if (!ValidationHelper.IsValidVIN(EditingCar.VIN))
                {
                    ErrorMessage = "Ошибка ввода: некорректный VIN (должен быть 17 символов)!";
                    return;
                }

                if (_model.VinExists(EditingCar.VIN, _isEditMode ? EditingCar.Id : (int?)null))
                {
                    ErrorMessage = "Автомобиль с таким VIN номером уже существует в базе!";
                    return;
                }
            }

            if (_model.RegistrationNumberExists(EditingCar.RegistrationNumber, _isEditMode ? EditingCar.Id : (int?)null))
            {
                ErrorMessage = "Автомобиль с таким государственным номером уже существует в базе!";
                return;
            }
            try
            {
                if (!_isEditMode)
                {
                    _model.CreateCar(EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber,
                        EditingCar.VIN, EditingCar.OwnerId);
                    SuccessMessage = "Автомобиль успешно добавлен!";
                }
                else
                {
                    _model.EditCar(EditingCar.Id, EditingCar.Brand, EditingCar.Model,
                        EditingCar.RegistrationNumber, EditingCar.VIN, EditingCar.OwnerId);
                    SuccessMessage = "Автомобиль успешно обновлен!";
                }

                CarSaved?.Invoke(this, EventArgs.Empty);

                await System.Threading.Tasks.Task.Delay(800);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
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
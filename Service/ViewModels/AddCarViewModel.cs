using Service.Data;
using Service.Models;
using Service.Utility;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddCarViewModel : BaseViewModel
    {
        private readonly CarAddEditModel _model = new CarAddEditModel();
        private Car _editingCar;
        private bool _isEditMode;
        private string _searchClientText;
        private ObservableCollection<Client> _allClients;
        private ObservableCollection<Client> _filteredClients;

        public event EventHandler CarSaved;

        public Car EditingCar
        {
            get => _editingCar;
            set
            {
                _editingCar = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Client> Clients
        {
            get => _filteredClients;
            private set
            {
                _filteredClients = value;
                OnPropertyChanged();
            }
        }

        public string SearchClientText
        {
            get => _searchClientText;
            set
            {
                _searchClientText = value;
                OnPropertyChanged();
                FilterClients();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddNewClientCommand { get; }

        private string _errorMessage = "";
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

        private void LoadClients()
        {
            _allClients = new ObservableCollection<Client>(_model.GetClients());
            FilterClients();
        }

        private void FilterClients()
        {
            if (string.IsNullOrWhiteSpace(SearchClientText))
                Clients = new ObservableCollection<Client>(_allClients);
            else
                Clients = new ObservableCollection<Client>(
                    _allClients.Where(c => c.FullName.ToLower().Contains(SearchClientText.ToLower())));
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(EditingCar.Brand) || string.IsNullOrWhiteSpace(EditingCar.Model))
            {
                ErrorMessage = "Заполните марку и модель автомобиля!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingCar.OwnerId == 0)
            {
                ErrorMessage = "Выберите владельца автомобиля!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
            {
                ErrorMessage = "Введите государственный номер автомобиля!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidationHelper.IsValidRussianLicensePlate(EditingCar.RegistrationNumber))
            {
                ErrorMessage = "Некорректный формат государственного номера!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingCar.VIN))
            {
                if (!ValidationHelper.IsValidVIN(EditingCar.VIN))
                {
                    ErrorMessage = "Некорректный VIN-номер! Должен содержать 17 символов.";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.VinExists(EditingCar.VIN, _isEditMode ? EditingCar.Id : (int?)null))
                {
                    ErrorMessage = "Автомобиль с таким VIN уже существует!";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (_model.RegistrationNumberExists(EditingCar.RegistrationNumber, _isEditMode ? EditingCar.Id : (int?)null))
            {
                ErrorMessage = "Автомобиль с таким государственным номером уже существует!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateCar(EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber,
                                   EditingCar.VIN, EditingCar.OwnerId);
                    CustomMessageBox.Show("Автомобиль успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditCar(EditingCar.Id, EditingCar.Brand, EditingCar.Model, EditingCar.RegistrationNumber,
                                 EditingCar.VIN, EditingCar.OwnerId);
                    CustomMessageBox.Show("Автомобиль успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CarSaved?.Invoke(this, EventArgs.Empty);

                // Закрываем окно после MessageBox
                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                CustomMessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewClient(object parameter)
        {
            var addClientWindow = new AddClient();
            var addClientViewModel = new AddClientViewModel();
            addClientWindow.DataContext = addClientViewModel;

            addClientViewModel.ClientSaved += OnClientSaved;

            if (addClientWindow.ShowDialog() == true)
                LoadClients();

            addClientViewModel.ClientSaved -= OnClientSaved;
        }

        private void OnClientSaved(object sender, EventArgs e) => LoadClients();

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
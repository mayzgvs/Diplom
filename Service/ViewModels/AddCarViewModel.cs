using Service.Data;
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
        private readonly ApplicationContext _context;
        private Car _editingCar;
        private bool _isEditMode;
        private ObservableCollection<Client> _clients;

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
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }
        public ICommand AddNewClientCommand { get; set; } // Добавляем команду

        public AddCarViewModel(ApplicationContext context, Car car = null)
        {
            _context = context;

            // Загружаем список клиентов
            Clients = new ObservableCollection<Client>(_context.Clients.ToList());

            if (car == null)
            {
                _isEditMode = false;
                EditingCar = new Car();
            }
            else
            {
                _isEditMode = true;
                EditingCar = new Car
                {
                    Id = car.Id,
                    Brand = car.Brand,
                    Model = car.Model,
                    RegistrationNumber = car.RegistrationNumber,
                    VIN = car.VIN,
                    OwnerId = car.OwnerId,
                    Client = car.Client
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewClientCommand = new RelayCommand(AddNewClient); // Инициализация команды
        }

        private void AddNewClient(object parameter)
        {
            var addClientWindow = new AddClient();
            var addClientViewModel = new AddClientViewModel(_context);
            addClientWindow.DataContext = addClientViewModel;

            if (addClientWindow.ShowDialog() == true)
            {
                // Обновляем список клиентов
                Clients = new ObservableCollection<Client>(_context.Clients.ToList());

                // Если нужно, можно автоматически выбрать нового клиента
                if (Clients.Count > 0)
                {
                    EditingCar.Client = Clients.LastOrDefault();
                    EditingCar.OwnerId = EditingCar.Client?.Id ?? 0;
                }
            }
        }

        private void Save(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditingCar.Brand))
                {
                    MessageBox.Show("Марка обязательна для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingCar.Model))
                {
                    MessageBox.Show("Модель обязательна для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
                {
                    MessageBox.Show("Регистрационный номер обязателен для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingCar.OwnerId == 0)
                {
                    MessageBox.Show("Выберите владельца автомобиля.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_isEditMode)
                {
                    _context.Cars.Add(EditingCar);
                }
                else
                {
                    var existing = _context.Cars.Find(EditingCar.Id);
                    if (existing != null)
                    {
                        existing.Brand = EditingCar.Brand;
                        existing.Model = EditingCar.Model;
                        existing.RegistrationNumber = EditingCar.RegistrationNumber;
                        existing.VIN = EditingCar.VIN;
                        existing.OwnerId = EditingCar.OwnerId;
                    }
                }

                _context.SaveChanges();

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
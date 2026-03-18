using Service.Data;
using Service.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity; 
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class CarViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context; 

        private ObservableCollection<Car> _cars;
        public ObservableCollection<Car> Cars
        {
            get => _cars;
            set
            {
                _cars = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Client> _clients;
        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged();
            }
        }

        // Выбранный автомобиль в DataGrid
        private Car _selectedCar;
        public Car SelectedCar
        {
            get => _selectedCar;
            set
            {
                _selectedCar = value;
                OnPropertyChanged();
                if (value != null)
                {
                    EditingCar = new Car
                    {
                        Id = value.Id,
                        Brand = value.Brand,
                        Model = value.Model,
                        RegistrationNumber = value.RegistrationNumber,
                        VIN = value.VIN,
                        OwnerId = value.OwnerId,
                        Client = value.Client
                    };
                }
                else
                {
                    EditingCar = null;
                }
            }
        }

        private Car _editingCar;
        public Car EditingCar
        {
            get => _editingCar;
            set
            {
                _editingCar = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }

        public CarViewModel()
        {
            _context = new ApplicationContext();
            Cars = new ObservableCollection<Car>();
            Clients = new ObservableCollection<Client>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewCar);
            EditCommand = new RelayCommand(EditCar, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveCarAsync(), CanSaveCar);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteCarAsync(), CanEditOrDelete);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var cars = await _context.Cars.Include(c => c.Client).ToListAsync();
                Cars = new ObservableCollection<Car>(cars);

                var clients = await _context.Clients.ToListAsync();
                Clients = new ObservableCollection<Client>(clients);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddNewCar(object obj)
        {
            EditingCar = new Car
            {
                OwnerId = Clients.FirstOrDefault()?.Id ?? 0 
            };
            SelectedCar = null; 
        }

        private void EditCar(object obj)
        {
            if (SelectedCar != null)
            {
                // Создаем копию для редактирования
                EditingCar = new Car
                {
                    Id = SelectedCar.Id,
                    Brand = SelectedCar.Brand,
                    Model = SelectedCar.Model,
                    RegistrationNumber = SelectedCar.RegistrationNumber,
                    VIN = SelectedCar.VIN,
                    OwnerId = SelectedCar.OwnerId,
                    Client = SelectedCar.Client
                };
            }
        }

        private async Task SaveCarAsync()
        {
            if (EditingCar == null) return;

            if (string.IsNullOrWhiteSpace(EditingCar.Brand) ||
                string.IsNullOrWhiteSpace(EditingCar.Model) ||
                string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
            {
                MessageBox.Show("Марка, модель и госномер обязательны для заполнения.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingCar.Id == 0) 
                {
                    _context.Cars.Add(EditingCar);
                    await _context.SaveChangesAsync();

                    await _context.Entry(EditingCar).Reference(c => c.Client).LoadAsync();
                    Cars.Add(EditingCar);
                }
                else 
                {
                    var carToUpdate = await _context.Cars.FindAsync(EditingCar.Id);
                    if (carToUpdate != null)
                    {
                        carToUpdate.Brand = EditingCar.Brand;
                        carToUpdate.Model = EditingCar.Model;
                        carToUpdate.RegistrationNumber = EditingCar.RegistrationNumber;
                        carToUpdate.VIN = EditingCar.VIN;
                        carToUpdate.OwnerId = EditingCar.OwnerId;

                        await _context.SaveChangesAsync();

                        var existingCar = Cars.FirstOrDefault(c => c.Id == EditingCar.Id);
                        if (existingCar != null)
                        {
                            existingCar.Brand = EditingCar.Brand;
                            existingCar.Model = EditingCar.Model;
                            existingCar.RegistrationNumber = EditingCar.RegistrationNumber;
                            existingCar.VIN = EditingCar.VIN;
                            existingCar.OwnerId = EditingCar.OwnerId;
                            existingCar.Client = Clients.FirstOrDefault(cl => cl.Id == EditingCar.OwnerId);
                        }
                    }
                }

                EditingCar = null;
                SelectedCar = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit(object obj)
        {
            EditingCar = null;
            SelectedCar = null;
        }

        private async Task DeleteCarAsync()
        {
            if (SelectedCar == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить автомобиль {SelectedCar.Brand} {SelectedCar.Model} ({SelectedCar.RegistrationNumber})?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var carToDelete = await _context.Cars.FindAsync(SelectedCar.Id);
                if (carToDelete != null)
                {
                    _context.Cars.Remove(carToDelete);
                    await _context.SaveChangesAsync();

                    Cars.Remove(SelectedCar);
                    SelectedCar = null;
                    EditingCar = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}. Возможно, на этот автомобиль ссылаются другие записи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanEditOrDelete(object obj)
        {
            return SelectedCar != null;
        }

        private bool CanSaveCar(object obj)
        {
            return EditingCar != null && !IsLoading;
        }
        private void FilterCars()
        {
            if (Cars == null) return;

            var filtered = Cars.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    (c.Brand?.ToLower().Contains(search) == true) ||
                    (c.Model?.ToLower().Contains(search) == true) ||
                    (c.RegistrationNumber?.ToLower().Contains(search) == true) ||
                    (c.VIN?.ToLower().Contains(search) == true));
            }

            FilteredCars = new ObservableCollection<Car>(filtered);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterCars(); // вызывать фильтрацию при изменении
            }
        }

        private ObservableCollection<Car> _filteredCars;
        public ObservableCollection<Car> FilteredCars
        {
            get => _filteredCars;
            set
            {
                _filteredCars = value;
                OnPropertyChanged();
            }
        }
    }
}
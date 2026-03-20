using Service.Data;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterCars();
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
        public ICommand ClearFiltersCommand { get; }

        public CarViewModel()
        {
            _context = new ApplicationContext();
            Cars = new ObservableCollection<Car>();
            FilteredCars = new ObservableCollection<Car>();
            Clients = new ObservableCollection<Client>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewCar);
            EditCommand = new RelayCommand(EditCar, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveCarAsync(), CanSaveCar);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteCarAsync(), CanEditOrDelete);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var cars = await _context.Cars
                    .Include(c => c.Client)
                    .OrderBy(c => c.Brand)
                    .ThenBy(c => c.Model)
                    .ToListAsync();
                Cars = new ObservableCollection<Car>(cars);

                var clients = await _context.Clients
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();
                Clients = new ObservableCollection<Client>(clients);

                FilteredCars = new ObservableCollection<Car>(Cars);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterCars()
        {
            if (Cars == null) return;

            var filtered = Cars.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    (c.Brand?.ToLower().Contains(searchLower) == true) ||
                    (c.Model?.ToLower().Contains(searchLower) == true) ||
                    (c.RegistrationNumber?.ToLower().Contains(searchLower) == true) ||
                    (c.VIN?.ToLower().Contains(searchLower) == true) ||
                    (c.Client?.FullName?.ToLower().Contains(searchLower) == true));
            }

            FilteredCars = new ObservableCollection<Car>(filtered);
        }

        private void ClearFilters(object obj)
        {
            SearchText = string.Empty;
        }

        private void AddNewCar(object obj)
        {
            var addWindow = new Views.AddCarView();
            var viewModel = new AddCarViewModel(_context);
            addWindow.DataContext = viewModel;

            if (addWindow.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditCar(object obj)
        {
            if (SelectedCar != null)
            {
                var editWindow = new Views.AddCarView();
                var viewModel = new AddCarViewModel(_context, SelectedCar);
                editWindow.DataContext = viewModel;

                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private async Task SaveCarAsync()
        {
            if (EditingCar == null) return;

            if (string.IsNullOrWhiteSpace(EditingCar.Brand) ||
                string.IsNullOrWhiteSpace(EditingCar.Model) ||
                string.IsNullOrWhiteSpace(EditingCar.RegistrationNumber))
            {
                MessageBox.Show("Марка, модель и госномер обязательны для заполнения.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingCar.Id == 0) // Новый автомобиль
                {
                    _context.Cars.Add(EditingCar);
                    await _context.SaveChangesAsync();

                    await _context.Entry(EditingCar).Reference(c => c.Client).LoadAsync();
                    Cars.Add(EditingCar);
                    FilteredCars.Add(EditingCar);
                }
                else // Редактирование существующего
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

                        FilterCars();
                    }
                }

                EditingCar = null;
                SelectedCar = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Проверяем наличие заявок на ремонт для этого автомобиля
            var hasRepairRequests = await _context.RepairRequests
                .AnyAsync(r => r.CarId == SelectedCar.Id);

            if (hasRepairRequests)
            {
                MessageBox.Show("Невозможно удалить автомобиль, на который есть заявки на ремонт.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить автомобиль {SelectedCar.Brand} {SelectedCar.Model} ({SelectedCar.RegistrationNumber})?",
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
                    FilteredCars.Remove(SelectedCar);
                    SelectedCar = null;
                    EditingCar = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
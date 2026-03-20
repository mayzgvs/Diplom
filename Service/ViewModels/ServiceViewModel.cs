using Service.Data;
using Service.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ServiceModel = Service.Data.Service; 
namespace Service.ViewModels
{
    public class ServiceViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        // Коллекция услуг для отображения в DataGrid
        private ObservableCollection<ServiceModel> _services;
        public ObservableCollection<ServiceModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                OnPropertyChanged();
            }
        }

        // Коллекция категорий для выпадающего списка
        private ObservableCollection<ServiceCategory> _categories;
        public ObservableCollection<ServiceCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        // Выбранная услуга в DataGrid
        private ServiceModel _selectedService;
        public ServiceModel SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged(nameof(SelectedService));
                // Копируем выбранную услугу для редактирования
                if (value != null)
                {
                    EditingService = new ServiceModel
                    {
                        Id = value.Id,
                        Name = value.Name,
                        Cost = value.Cost,
                        ServiceCategoryId = value.ServiceCategoryId,
                        ServiceCategory = value.ServiceCategory
                    };
                }
                else
                {
                    EditingService = null;
                }
            }
        }

        // Услуга, которая редактируется в данный момент
        private ServiceModel _editingService;
        public ServiceModel EditingService
        {
            get => _editingService;
            set
            {
                _editingService = value;
                OnPropertyChanged(nameof(EditingService));
            }
        }

        // Режим редактирования
        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }

        // Состояние загрузки данных
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // Статистика
        private int _totalServices;
        public int TotalServices
        {
            get => _totalServices;
            set
            {
                _totalServices = value;
                OnPropertyChanged(nameof(TotalServices));
            }
        }

        private decimal _averageCost;
        public decimal AverageCost
        {
            get => _averageCost;
            set
            {
                _averageCost = value;
                OnPropertyChanged(nameof(AverageCost));
            }
        }

        private decimal _minCost;
        public decimal MinCost
        {
            get => _minCost;
            set
            {
                _minCost = value;
                OnPropertyChanged(nameof(MinCost));
            }
        }

        private decimal _maxCost;
        public decimal MaxCost
        {
            get => _maxCost;
            set
            {
                _maxCost = value;
                OnPropertyChanged(nameof(MaxCost));
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ManageCategoriesCommand { get; }

        public ServiceViewModel()
        {
            _context = new ApplicationContext();
            Services = new ObservableCollection<ServiceModel>();
            Categories = new ObservableCollection<ServiceCategory>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewService);
            EditCommand = new RelayCommand(EditService, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveServiceAsync(), CanSaveService);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteServiceAsync(), CanEditOrDelete);
            RefreshCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            ManageCategoriesCommand = new RelayCommand(ManageCategories);
        }

        // Загрузка данных из БД
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Загружаем услуги вместе с категориями
                var services = await _context.Services
                    .Include(s => s.ServiceCategory)
                    .OrderBy(s => s.ServiceCategory.Name)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
                Services = new ObservableCollection<ServiceModel>(services);

                // Загружаем категории
                var categories = await _context.ServiceCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                Categories = new ObservableCollection<ServiceCategory>(categories);

                // Обновляем статистику
                UpdateStatistics();
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

        // Обновление статистики
        private void UpdateStatistics()
        {
            if (Services.Any())
            {
                TotalServices = Services.Count;
                AverageCost = Math.Round(Services.Average(s => s.Cost), 2);
                MinCost = Services.Min(s => s.Cost);
                MaxCost = Services.Max(s => s.Cost);
            }
            else
            {
                TotalServices = 0;
                AverageCost = 0;
                MinCost = 0;
                MaxCost = 0;
            }
        }

        private void AddNewService(object obj)
        {
            var addWindow = new Views.AddServiceView();
            var viewModel = new AddServiceViewModel(_context);
            addWindow.DataContext = viewModel;

            if (addWindow.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditService(object obj)
        {
            if (SelectedService != null)
            {
                var editWindow = new Views.AddServiceView();
                var viewModel = new AddServiceViewModel(_context, SelectedService);
                editWindow.DataContext = viewModel;

                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private async Task SaveServiceAsync()
        {
            if (EditingService == null) return;

            if (string.IsNullOrWhiteSpace(EditingService.Name))
            {
                MessageBox.Show("Наименование услуги обязательно для заполнения.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingService.Cost < 0)
            {
                MessageBox.Show("Стоимость услуги не может быть отрицательной.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingService.ServiceCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию услуги.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingService.Id == 0) 
                {
                    _context.Services.Add(EditingService);
                    await _context.SaveChangesAsync();

                    await _context.Entry(EditingService)
                        .Reference(s => s.ServiceCategory).LoadAsync();
                    Services.Add(EditingService);
                }
                else
                {
                    var serviceToUpdate = await _context.Services
                        .FindAsync(EditingService.Id);
                    if (serviceToUpdate != null)
                    {
                        serviceToUpdate.Name = EditingService.Name;
                        serviceToUpdate.Cost = EditingService.Cost;
                        serviceToUpdate.ServiceCategoryId = EditingService.ServiceCategoryId;

                        await _context.SaveChangesAsync();

                        var existingService = Services
                            .FirstOrDefault(s => s.Id == EditingService.Id);
                        if (existingService != null)
                        {
                            existingService.Name = EditingService.Name;
                            existingService.Cost = EditingService.Cost;
                            existingService.ServiceCategoryId = EditingService.ServiceCategoryId;
                            existingService.ServiceCategory = Categories
                                .FirstOrDefault(c => c.Id == EditingService.ServiceCategoryId);
                        }
                    }
                }

                UpdateStatistics();

                EditingService = null;
                SelectedService = null;
                IsEditMode = false;
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
            EditingService = null;
            SelectedService = null;
            IsEditMode = false;
        }

        private async Task DeleteServiceAsync()
        {
            if (SelectedService == null) return;

            var isUsed = await _context.WorkItems
                .AnyAsync(w => w.ServiceId == SelectedService.Id);

            if (isUsed)
            {
                MessageBox.Show("Невозможно удалить услугу, которая используется в работах.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить услугу '{SelectedService.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var serviceToDelete = await _context.Services
                    .FindAsync(SelectedService.Id);
                if (serviceToDelete != null)
                {
                    _context.Services.Remove(serviceToDelete);
                    await _context.SaveChangesAsync();

                    Services.Remove(SelectedService);
                    SelectedService = null;
                    EditingService = null;
                    IsEditMode = false;

                    UpdateStatistics();
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

        private void ManageCategories(object obj)
        {
            MessageBox.Show("Управление категориями услуг будет доступно в следующей версии.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanEditOrDelete(object obj)
        {
            return SelectedService != null;
        }

        private bool CanSaveService(object obj)
        {
            return EditingService != null && !IsLoading;
        }
    }
}
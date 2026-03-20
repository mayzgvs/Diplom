using Service.Data;
using Service.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class RepairRequestViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        // Коллекция заявок
        private ObservableCollection<RepairRequest> _repairRequests;
        public ObservableCollection<RepairRequest> RepairRequests
        {
            get => _repairRequests;
            set
            {
                _repairRequests = value;
                OnPropertyChanged();
            }
        }

        // Коллекция автомобилей для выпадающего списка
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

        // Коллекция статусов для выпадающего списка
        private ObservableCollection<StatusRequest> _statuses;
        public ObservableCollection<StatusRequest> Statuses
        {
            get => _statuses;
            set
            {
                _statuses = value;
                OnPropertyChanged();
            }
        }

        // Выбранная заявка
        private RepairRequest _selectedRepairRequest;
        public RepairRequest SelectedRepairRequest
        {
            get => _selectedRepairRequest;
            set
            {
                _selectedRepairRequest = value;
                OnPropertyChanged();
                // Копируем выбранную заявку для редактирования
                if (value != null)
                {
                    EditingRepairRequest = new RepairRequest
                    {
                        Id = value.Id,
                        CarId = value.CarId,
                        Client = value.Client,
                        StartDate = value.StartDate,
                        EndDate = value.EndDate,
                        TotalCost = value.TotalCost,
                        StatusId = value.StatusId,
                        Car = value.Car,
                        Status = value.Status
                    };
                }
                else
                {
                    EditingRepairRequest = null;
                }
            }
        }

        // Заявка, которая редактируется в данный момент
        private RepairRequest _editingRepairRequest;
        public RepairRequest EditingRepairRequest
        {
            get => _editingRepairRequest;
            set
            {
                _editingRepairRequest = value;
                OnPropertyChanged();

                // При изменении автомобиля автоматически подставляем клиента
                if (value != null && value.CarId > 0)
                {
                    var selectedCar = Cars?.FirstOrDefault(c => c.Id == value.CarId);
                    if (selectedCar?.Client != null)
                    {
                        value.Client = selectedCar.Client.FullName;
                    }
                }
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        // Статистика
        private int _activeRequests;
        public int ActiveRequests
        {
            get => _activeRequests;
            set
            {
                _activeRequests = value;
                OnPropertyChanged();
            }
        }

        private int _completedRequests;
        public int CompletedRequests
        {
            get => _completedRequests;
            set
            {
                _completedRequests = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged();
            }
        }

        // Фильтрация
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterRequests();
            }
        }

        private StatusRequest _selectedFilterStatus;
        public StatusRequest SelectedFilterStatus
        {
            get => _selectedFilterStatus;
            set
            {
                _selectedFilterStatus = value;
                OnPropertyChanged();
                FilterRequests();
            }
        }

        private ObservableCollection<RepairRequest> _filteredRequests;
        public ObservableCollection<RepairRequest> FilteredRequests
        {
            get => _filteredRequests;
            set
            {
                _filteredRequests = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand CarSelectionChangedCommand { get; }

        public RepairRequestViewModel()
        {
            _context = new ApplicationContext();
            RepairRequests = new ObservableCollection<RepairRequest>();
            Cars = new ObservableCollection<Car>();
            Statuses = new ObservableCollection<StatusRequest>();
            FilteredRequests = new ObservableCollection<RepairRequest>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewRepairRequest);
            EditCommand = new RelayCommand(EditRepairRequest, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveRepairRequestAsync(), CanSaveRepairRequest);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteRepairRequestAsync(), CanEditOrDelete);
            RefreshCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            ClearFilterCommand = new RelayCommand(ClearFilter);
            CarSelectionChangedCommand = new RelayCommand(OnCarSelectionChanged);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Загружаем заявки с навигационными свойствами
                var requests = await _context.RepairRequests
                    .Include(r => r.Car)
                    .Include(r => r.Car.Client)
                    .Include(r => r.Status)
                    .OrderByDescending(r => r.StartDate)
                    .ToListAsync();
                RepairRequests = new ObservableCollection<RepairRequest>(requests);

                // Загружаем автомобили с клиентами
                var cars = await _context.Cars
                    .Include(c => c.Client)
                    .OrderBy(c => c.Brand)
                    .ThenBy(c => c.Model)
                    .ToListAsync();
                Cars = new ObservableCollection<Car>(cars);

                // Загружаем статусы
                var statuses = await _context.StatusRequests
                    .OrderBy(s => s.Id)
                    .ToListAsync();
                Statuses = new ObservableCollection<StatusRequest>(statuses);

                ApplyFilter();

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

        private void UpdateStatistics()
        {
            try
            {
                // Активные заявки (статус не "Завершена" и не "Отменена")
                // ID статусов: 1 - Новая, 2 - В работе, 3 - Завершена, 4 - Отменена
                ActiveRequests = RepairRequests.Count(r => r.StatusId == 1 || r.StatusId == 2);

                // Завершенные заявки
                CompletedRequests = RepairRequests.Count(r => r.StatusId == 3);

                // Общая выручка по завершенным заявкам
                TotalRevenue = RepairRequests
                    .Where(r => r.StatusId == 3 && r.TotalCost > 0)
                    .Sum(r => r.TotalCost);
            }
            catch
            {
                ActiveRequests = 0;
                CompletedRequests = 0;
                TotalRevenue = 0;
            }
        }

        // Фильтрация заявок
        private void FilterRequests()
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (RepairRequests == null) return;

            var query = RepairRequests.AsEnumerable();

            // Фильтр по тексту (номер авто, клиент)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(r =>
                    (r.Car != null && (
                        r.Car.RegistrationNumber?.ToLower().Contains(searchLower) == true ||
                        r.Car.Brand?.ToLower().Contains(searchLower) == true ||
                        r.Car.Model?.ToLower().Contains(searchLower) == true)) ||
                    (r.Client?.ToLower().Contains(searchLower) == true)
                );
            }

            // Фильтр по статусу
            if (SelectedFilterStatus != null)
            {
                query = query.Where(r => r.StatusId == SelectedFilterStatus.Id);
            }

            FilteredRequests = new ObservableCollection<RepairRequest>(query.OrderByDescending(r => r.StartDate));
        }

        private void ClearFilter(object obj)
        {
            SearchText = string.Empty;
            SelectedFilterStatus = null;
            ApplyFilter();
        }

        private void AddNewRepairRequest(object obj)
        {
            var addWindow = new Views.AddRepairView();
            var viewModel = new AddRepairRequestViewModel(_context);
            addWindow.DataContext = viewModel;

            if (addWindow.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditRepairRequest(object obj)
        {
            if (SelectedRepairRequest != null)
            {
                var editWindow = new Views.AddRepairView();
                var viewModel = new AddRepairRequestViewModel(_context, SelectedRepairRequest);
                editWindow.DataContext = viewModel;

                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private void OnCarSelectionChanged(object obj)
        {
            if (EditingRepairRequest != null && EditingRepairRequest.CarId > 0)
            {
                var selectedCar = Cars.FirstOrDefault(c => c.Id == EditingRepairRequest.CarId);
                if (selectedCar?.Client != null)
                {
                    EditingRepairRequest.Client = selectedCar.Client.FullName;
                }
            }
        }

        private async Task SaveRepairRequestAsync()
        {
            if (EditingRepairRequest == null) return;

            if (EditingRepairRequest.CarId == 0)
            {
                MessageBox.Show("Выберите автомобиль.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingRepairRequest.Client))
            {
                MessageBox.Show("Клиент должен быть указан.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.StartDate == DateTime.MinValue)
            {
                MessageBox.Show("Укажите дату начала работ.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.EndDate.HasValue &&
                EditingRepairRequest.EndDate.Value < EditingRepairRequest.StartDate)
            {
                MessageBox.Show("Дата завершения не может быть раньше даты начала.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingRepairRequest.StatusId == 0)
            {
                MessageBox.Show("Выберите статус заявки.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingRepairRequest.Id == 0) 
                {
                    _context.RepairRequests.Add(EditingRepairRequest);
                    await _context.SaveChangesAsync();

                    await _context.Entry(EditingRepairRequest)
                        .Reference(r => r.Car).Query().Include(c => c.Client).LoadAsync();
                    await _context.Entry(EditingRepairRequest)
                        .Reference(r => r.Status).LoadAsync();

                    RepairRequests.Insert(0, EditingRepairRequest); 
                }
                else 
                {
                    var requestToUpdate = await _context.RepairRequests
                        .FindAsync(EditingRepairRequest.Id);
                    if (requestToUpdate != null)
                    {
                        requestToUpdate.CarId = EditingRepairRequest.CarId;
                        requestToUpdate.Client = EditingRepairRequest.Client;
                        requestToUpdate.StartDate = EditingRepairRequest.StartDate;
                        requestToUpdate.EndDate = EditingRepairRequest.EndDate;
                        requestToUpdate.TotalCost = EditingRepairRequest.TotalCost;
                        requestToUpdate.StatusId = EditingRepairRequest.StatusId;

                        await _context.SaveChangesAsync();

                        var existingRequest = RepairRequests
                            .FirstOrDefault(r => r.Id == EditingRepairRequest.Id);
                        if (existingRequest != null)
                        {
                            existingRequest.CarId = EditingRepairRequest.CarId;
                            existingRequest.Client = EditingRepairRequest.Client;
                            existingRequest.StartDate = EditingRepairRequest.StartDate;
                            existingRequest.EndDate = EditingRepairRequest.EndDate;
                            existingRequest.TotalCost = EditingRepairRequest.TotalCost;
                            existingRequest.StatusId = EditingRepairRequest.StatusId;
                            existingRequest.Car = Cars.FirstOrDefault(c => c.Id == EditingRepairRequest.CarId);
                            existingRequest.Status = Statuses.FirstOrDefault(s => s.Id == EditingRepairRequest.StatusId);
                        }
                    }
                }

                UpdateStatistics();

                ApplyFilter();

                EditingRepairRequest = null;
                SelectedRepairRequest = null;
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
            EditingRepairRequest = null;
            SelectedRepairRequest = null;
            IsEditMode = false;
        }

        private async Task DeleteRepairRequestAsync()
        {
            if (SelectedRepairRequest == null) return;

            var hasWorkItems = await _context.WorkItems
                .AnyAsync(w => w.RepairRequestId == SelectedRepairRequest.Id);

            if (hasWorkItems)
            {
                MessageBox.Show("Невозможно удалить заявку, в которой есть работы. " +
                    "Сначала удалите все работы из заявки.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заявку #{SelectedRepairRequest.Id}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var requestToDelete = await _context.RepairRequests
                    .FindAsync(SelectedRepairRequest.Id);
                if (requestToDelete != null)
                {
                    _context.RepairRequests.Remove(requestToDelete);
                    await _context.SaveChangesAsync();

                    RepairRequests.Remove(SelectedRepairRequest);
                    FilteredRequests.Remove(SelectedRepairRequest);
                    SelectedRepairRequest = null;
                    EditingRepairRequest = null;
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

        private bool CanEditOrDelete(object obj)
        {
            return SelectedRepairRequest != null;
        }

        private bool CanSaveRepairRequest(object obj)
        {
            return EditingRepairRequest != null && !IsLoading;
        }
    }
}
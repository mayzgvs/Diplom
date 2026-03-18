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
    public class EmployeeViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        // Коллекция для отображения в DataGrid (ВСЕ сотрудники)
        private ObservableCollection<Employee> _employees;
        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                OnPropertyChanged();
                // При изменении списка всех сотрудников обновляем и фильтрованный список
                FilterEmployees();
            }
        }

        // ОТФИЛЬТРОВАННАЯ коллекция для отображения в DataGrid
        private ObservableCollection<Employee> _filteredEmployees;
        public ObservableCollection<Employee> FilteredEmployees
        {
            get => _filteredEmployees;
            set
            {
                _filteredEmployees = value;
                OnPropertyChanged();
            }
        }

        // Текст поиска (привязан к TextBox в XAML)
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterEmployees(); // Фильтруем при каждом изменении текста
            }
        }

        // Выбранный сотрудник в DataGrid
        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();

                // Копируем выбранного сотрудника для редактирования
                if (value != null)
                {
                    EditingEmployee = new Employee
                    {
                        Id = value.Id,
                        FirstName = value.FirstName,
                        LastName = value.LastName,
                        ContactNumber = value.ContactNumber
                    };
                }
                else
                {
                    EditingEmployee = null;
                }
            }
        }

        // Сотрудник, который редактируется в данный момент
        private Employee _editingEmployee;
        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set
            {
                _editingEmployee = value;
                OnPropertyChanged();
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
        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            set
            {
                _totalEmployees = value;
                OnPropertyChanged();
            }
        }

        private int _activeEmployees;
        public int ActiveEmployees
        {
            get => _activeEmployees;
            set
            {
                _activeEmployees = value;
                OnPropertyChanged();
            }
        }

        // Команды
        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearSearchCommand { get; } // Новая команда для очистки поиска

        public EmployeeViewModel()
        {
            _context = new ApplicationContext();
            Employees = new ObservableCollection<Employee>();
            FilteredEmployees = new ObservableCollection<Employee>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewEmployee);
            EditCommand = new RelayCommand(EditEmployee, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveEmployeeAsync(), CanSaveEmployee);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteEmployeeAsync(), CanEditOrDelete);
            RefreshCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            ClearSearchCommand = new RelayCommand(ClearSearch); // Инициализация команды очистки
        }

        // Загрузка данных из БД
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Загружаем сотрудников
                var employees = await _context.Employees
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                Employees = new ObservableCollection<Employee>(employees);

                // После загрузки применяем фильтрацию (пока текст поиска пустой)
                FilterEmployees();

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

        // Метод фильтрации сотрудников
        private void FilterEmployees()
        {
            if (Employees == null) return;

            // Начинаем со всех сотрудников
            var filtered = Employees.AsEnumerable();

            // Если есть текст для поиска - фильтруем
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower().Trim();
                filtered = filtered.Where(e =>
                    (e.LastName?.ToLower().Contains(searchLower) == true) ||
                    (e.FirstName?.ToLower().Contains(searchLower) == true) ||
                    (e.ContactNumber?.ToLower().Contains(searchLower) == true)
                );
            }

            // Сортируем отфильтрованные результаты
            filtered = filtered.OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

            // Создаем новую отфильтрованную коллекцию
            FilteredEmployees = new ObservableCollection<Employee>(filtered);

            // Обновляем заголовок с количеством найденных записей
            OnPropertyChanged(nameof(FilteredEmployees));
        }

        // Очистка поиска
        private void ClearSearch(object obj)
        {
            SearchText = string.Empty; // Это автоматически вызовет FilterEmployees()
        }

        // Обновление статистики
        private async void UpdateStatistics()
        {
            try
            {
                TotalEmployees = Employees.Count;

                // Подсчет активных сотрудников (у которых есть незавершенные работы)
                ActiveEmployees = await _context.WorkItems
                    .Where(w => w.EmployeeId != null)
                    .Select(w => w.EmployeeId)
                    .Distinct()
                    .CountAsync();
            }
            catch
            {
                ActiveEmployees = 0;
            }
        }

        // Добавление нового сотрудника
        private void AddNewEmployee(object obj)
        {
            EditingEmployee = new Employee();
            SelectedEmployee = null;
            IsEditMode = true;
        }

        // Редактирование сотрудника
        private void EditEmployee(object obj)
        {
            IsEditMode = true;
        }

        // Сохранение сотрудника
        private async Task SaveEmployeeAsync()
        {
            if (EditingEmployee == null) return;

            // Валидация
            if (string.IsNullOrWhiteSpace(EditingEmployee.LastName))
            {
                MessageBox.Show("Фамилия обязательна для заполнения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
            {
                MessageBox.Show("Имя обязательно для заполнения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка телефона (если указан)
            if (!string.IsNullOrWhiteSpace(EditingEmployee.ContactNumber))
            {
                var phone = EditingEmployee.ContactNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[0-9]{10,15}$"))
                {
                    MessageBox.Show("Введите корректный номер телефона.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            IsLoading = true;
            try
            {
                if (EditingEmployee.Id == 0) // Новый сотрудник
                {
                    _context.Employees.Add(EditingEmployee);
                    await _context.SaveChangesAsync();

                    // Добавляем в общую коллекцию
                    Employees.Add(EditingEmployee);
                }
                else // Редактирование существующего
                {
                    var employeeToUpdate = await _context.Employees.FindAsync(EditingEmployee.Id);
                    if (employeeToUpdate != null)
                    {
                        employeeToUpdate.FirstName = EditingEmployee.FirstName;
                        employeeToUpdate.LastName = EditingEmployee.LastName;
                        employeeToUpdate.ContactNumber = EditingEmployee.ContactNumber;

                        await _context.SaveChangesAsync();

                        // Обновляем в коллекции
                        var existingEmployee = Employees.FirstOrDefault(e => e.Id == EditingEmployee.Id);
                        if (existingEmployee != null)
                        {
                            existingEmployee.FirstName = EditingEmployee.FirstName;
                            existingEmployee.LastName = EditingEmployee.LastName;
                            existingEmployee.ContactNumber = EditingEmployee.ContactNumber;
                        }
                    }
                }

                // Обновляем отфильтрованный список
                FilterEmployees();

                // Обновляем статистику
                UpdateStatistics();

                // Выходим из режима редактирования
                EditingEmployee = null;
                SelectedEmployee = null;
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

        // Отмена редактирования
        private void CancelEdit(object obj)
        {
            EditingEmployee = null;
            SelectedEmployee = null;
            IsEditMode = false;
        }

        // Удаление сотрудника
        private async Task DeleteEmployeeAsync()
        {
            if (SelectedEmployee == null) return;

            // Проверяем, есть ли у сотрудника назначенные работы
            var hasWorkItems = await _context.WorkItems
                .AnyAsync(w => w.EmployeeId == SelectedEmployee.Id);

            if (hasWorkItems)
            {
                MessageBox.Show("Невозможно удалить сотрудника, у которого есть назначенные работы. " +
                    "Сначала переназначьте или завершите его работы.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить сотрудника {SelectedEmployee.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var employeeToDelete = await _context.Employees.FindAsync(SelectedEmployee.Id);
                if (employeeToDelete != null)
                {
                    _context.Employees.Remove(employeeToDelete);
                    await _context.SaveChangesAsync();

                    // Удаляем из общей коллекции
                    Employees.Remove(SelectedEmployee);

                    // Обновляем отфильтрованный список
                    FilterEmployees();

                    // Очищаем выделение
                    SelectedEmployee = null;
                    EditingEmployee = null;
                    IsEditMode = false;

                    // Обновляем статистику
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

        // Проверка возможности редактирования/удаления
        private bool CanEditOrDelete(object obj)
        {
            return SelectedEmployee != null;
        }

        // Проверка возможности сохранения
        private bool CanSaveEmployee(object obj)
        {
            return EditingEmployee != null && !IsLoading;
        }
    }
}
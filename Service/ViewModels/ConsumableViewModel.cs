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
    public class ConsumableViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        // Коллекция расходников для отображения в DataGrid
        private ObservableCollection<Consumable> _consumables;
        public ObservableCollection<Consumable> Consumables
        {
            get => _consumables;
            set
            {
                _consumables = value;
                OnPropertyChanged();
            }
        }

        // Коллекция категорий для выпадающего списка
        private ObservableCollection<ConsumablesCategory> _categories;
        public ObservableCollection<ConsumablesCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        // Выбранный расходник в DataGrid
        private Consumable _selectedConsumable;
        public Consumable SelectedConsumable
        {
            get => _selectedConsumable;
            set
            {
                _selectedConsumable = value;
                OnPropertyChanged();
                // Копируем выбранный расходник для редактирования
                if (value != null)
                {
                    EditingConsumable = new Consumable
                    {
                        Id = value.Id,
                        Name = value.Name,
                        ConsumableCategoryId = value.ConsumableCategoryId,
                        ConsumableCategory = value.ConsumableCategory
                    };
                }
                else
                {
                    EditingConsumable = null;
                }
            }
        }

        // Расходник, который редактируется в данный момент
        private Consumable _editingConsumable;
        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set
            {
                _editingConsumable = value;
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

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ManageCategoriesCommand { get; }

        public ConsumableViewModel()
        {
            _context = new ApplicationContext();
            Consumables = new ObservableCollection<Consumable>();
            Categories = new ObservableCollection<ConsumablesCategory>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewConsumable);
            EditCommand = new RelayCommand(EditConsumable, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveConsumableAsync(), CanSaveConsumable);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteConsumableAsync(), CanEditOrDelete);
            ManageCategoriesCommand = new RelayCommand(ManageCategories);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Загружаем расходники вместе с категориями
                var consumables = await _context.Consumables
                    .Include(c => c.ConsumableCategory)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                Consumables = new ObservableCollection<Consumable>(consumables);

                // Загружаем категории
                var categories = await _context.ConsumablesCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                Categories = new ObservableCollection<ConsumablesCategory>(categories);
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

        private void AddNewConsumable(object obj)
        {
            EditingConsumable = new Consumable
            {
                ConsumableCategoryId = Categories.FirstOrDefault()?.Id ?? 0
            };
            SelectedConsumable = null;
            IsEditMode = true;
        }

        private void EditConsumable(object obj)
        {
            IsEditMode = true;
        }

        private async Task SaveConsumableAsync()
        {
            if (EditingConsumable == null) return;

            if (string.IsNullOrWhiteSpace(EditingConsumable.Name))
            {
                MessageBox.Show("Наименование расходника обязательно для заполнения.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingConsumable.ConsumableCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию расходника.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingConsumable.Id == 0) 
                {
                    _context.Consumables.Add(EditingConsumable);
                    await _context.SaveChangesAsync();

                    await _context.Entry(EditingConsumable)
                        .Reference(c => c.ConsumableCategory).LoadAsync();
                    Consumables.Add(EditingConsumable);
                }
                else 
                {
                    var consumableToUpdate = await _context.Consumables
                        .FindAsync(EditingConsumable.Id);
                    if (consumableToUpdate != null)
                    {
                        consumableToUpdate.Name = EditingConsumable.Name;
                        consumableToUpdate.ConsumableCategoryId = EditingConsumable.ConsumableCategoryId;

                        await _context.SaveChangesAsync();

                        var existingConsumable = Consumables
                            .FirstOrDefault(c => c.Id == EditingConsumable.Id);
                        if (existingConsumable != null)
                        {
                            existingConsumable.Name = EditingConsumable.Name;
                            existingConsumable.ConsumableCategoryId = EditingConsumable.ConsumableCategoryId;
                            existingConsumable.ConsumableCategory = Categories
                                .FirstOrDefault(c => c.Id == EditingConsumable.ConsumableCategoryId);
                        }
                    }
                }

                EditingConsumable = null;
                SelectedConsumable = null;
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
            EditingConsumable = null;
            SelectedConsumable = null;
            IsEditMode = false;
        }

        private async Task DeleteConsumableAsync()
        {
            if (SelectedConsumable == null) return;

            var isUsed = await _context.WorkItems
                .AnyAsync(w => w.ConsumableId == SelectedConsumable.Id);

            if (isUsed)
            {
                MessageBox.Show("Невозможно удалить расходник, который используется в работах.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить расходник '{SelectedConsumable.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var consumableToDelete = await _context.Consumables
                    .FindAsync(SelectedConsumable.Id);
                if (consumableToDelete != null)
                {
                    _context.Consumables.Remove(consumableToDelete);
                    await _context.SaveChangesAsync();

                    Consumables.Remove(SelectedConsumable);
                    SelectedConsumable = null;
                    EditingConsumable = null;
                    IsEditMode = false;
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
            MessageBox.Show("Управление категориями будет доступно в следующей версии.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanEditOrDelete(object obj)
        {
            return SelectedConsumable != null;
        }

        private bool CanSaveConsumable(object obj)
        {
            return EditingConsumable != null && !IsLoading;
        }
    }
}
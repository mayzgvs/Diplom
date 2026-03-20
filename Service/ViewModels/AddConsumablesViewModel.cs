using Service.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddConsumablesViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private Consumable _editingConsumable;
        private bool _isEditMode;
        private ObservableCollection<ConsumablesCategory> _consumableCategories;

        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set
            {
                _editingConsumable = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ConsumablesCategory> ConsumableCategories
        {
            get => _consumableCategories;
            set
            {
                _consumableCategories = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }
        public ICommand AddCategoryCommand { get; set; }

        public AddConsumablesViewModel(ApplicationContext context, Consumable consumable = null)
        {
            _context = context;

            ConsumableCategories = new ObservableCollection<ConsumablesCategory>(_context.ConsumablesCategories.ToList());

            if (consumable == null)
            {
                _isEditMode = false;
                EditingConsumable = new Consumable();
            }
            else
            {
                _isEditMode = true;
                EditingConsumable = new Consumable
                {
                    Id = consumable.Id,
                    Name = consumable.Name,
                    ConsumableCategoryId = consumable.ConsumableCategoryId,
                    ConsumableCategory = consumable.ConsumableCategory
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddCategoryCommand = new RelayCommand(AddNewCategory);
        }

        private void AddNewCategory(object parameter)
        {
            MessageBox.Show("Добавление новой категории будет доступно в следующей версии.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditingConsumable.Name))
                {
                    MessageBox.Show("Наименование расходника обязательно для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingConsumable.ConsumableCategoryId == 0)
                {
                    MessageBox.Show("Выберите категорию расходника.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_isEditMode)
                {
                    _context.Consumables.Add(EditingConsumable);
                }
                else
                {
                    var existing = _context.Consumables.Find(EditingConsumable.Id);
                    if (existing != null)
                    {
                        existing.Name = EditingConsumable.Name;
                        existing.ConsumableCategoryId = EditingConsumable.ConsumableCategoryId;
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
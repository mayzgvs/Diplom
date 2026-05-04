using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddConsumablesViewModel : BaseViewModel
    {
        private readonly ConsumableAddEditModel _model = new ConsumableAddEditModel();
        private Consumable _editingConsumable;
        private bool _isEditMode;

        private string _searchCategoryText = string.Empty;
        private ObservableCollection<ConsumablesCategory> _allCategories;

        public event EventHandler ConsumableSaved;

        public string Title => _isEditMode ? "Редактирование расходника" : "Добавление расходника";

        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set { _editingConsumable = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ConsumablesCategory> _consumableCategories;
        public ObservableCollection<ConsumablesCategory> ConsumableCategories
        {
            get => _consumableCategories;
            private set
            {
                _consumableCategories = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public string SearchCategoryText
        {
            get => _searchCategoryText;
            set
            {
                _searchCategoryText = value;
                OnPropertyChanged();
                FilterCategories();
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private string _errorMessage;
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

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                _successMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSuccess));
            }
        }

        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public AddConsumablesViewModel(Consumable consumable = null)
        {
            LoadCategories();

            if (consumable == null)
            {
                _isEditMode = false;
                EditingConsumable = new Consumable();
            }
            else
            {
                _isEditMode = true;
                EditingConsumable = consumable;
                SetSearchTextForEditMode();
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void LoadCategories()
        {
            var categories = _model.GetCategories();
            _allCategories = new ObservableCollection<ConsumablesCategory>(categories.OrderBy(c => c.Name));
            FilterCategories();
        }

        private void SetSearchTextForEditMode()
        {
            if (_isEditMode && EditingConsumable?.ConsumableCategoryId > 0)
            {
                var category = _allCategories.FirstOrDefault(c => c.Id == EditingConsumable.ConsumableCategoryId);
                if (category != null)
                {
                    SearchCategoryText = category.Name;
                }
            }
        }

        private void FilterCategories()
        {
            if (string.IsNullOrWhiteSpace(SearchCategoryText))
            {
                ConsumableCategories = new ObservableCollection<ConsumablesCategory>(_allCategories);
            }
            else
            {
                var search = SearchCategoryText.ToLower();
                ConsumableCategories = new ObservableCollection<ConsumablesCategory>(
                    _allCategories.Where(c => c.Name.ToLower().Contains(search)));
            }
        }
        private void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingConsumable?.Name?.Trim()))
            {
                ErrorMessage = "Введите наименование расходника!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingConsumable.ConsumableCategoryId == 0)
            {
                ErrorMessage = "Выберите категорию расходника!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_model.ConsumableNameExistsInCategory(
                    EditingConsumable.Name.Trim(),
                    EditingConsumable.ConsumableCategoryId,
                    _isEditMode ? EditingConsumable.Id : (int?)null))
            {
                ErrorMessage = "Расходник с таким наименованием уже существует в выбранной категории!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateConsumable(EditingConsumable.Name.Trim(), EditingConsumable.ConsumableCategoryId, EditingConsumable.Cost);
                    CustomMessageBox.Show("Расходник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditConsumable(EditingConsumable.Id, EditingConsumable.Name.Trim(), EditingConsumable.ConsumableCategoryId, EditingConsumable.Cost);
                    CustomMessageBox.Show("Расходник успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ConsumableSaved?.Invoke(this, EventArgs.Empty);

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

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
using Service.Data;
using Service.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddConsumablesViewModel : BaseViewModel
    {
        private readonly ConsumableAddEditModel _model = new ConsumableAddEditModel();
        private Consumable _editingConsumable;
        private bool _isEditMode;

        public event EventHandler ConsumableSaved;

        public string Title => _isEditMode ? "Редактирование расходника" : "Добавление расходника";

        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set { _editingConsumable = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConsumablesCategory> ConsumableCategories { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
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
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void LoadCategories()
        {
            ConsumableCategories = new ObservableCollection<ConsumablesCategory>(_model.GetCategories());
        }

        private async void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingConsumable.Name))
            {
                ErrorMessage = "Введите наименование расходника!";
                return;
            }

            if (EditingConsumable.ConsumableCategoryId == 0)
            {
                ErrorMessage = "Выберите категорию расходника!";
                return;
            }

            if (_model.ConsumableNameExistsInCategory(EditingConsumable.Name, EditingConsumable.ConsumableCategoryId,
                _isEditMode ? EditingConsumable.Id : (int?)null))
            {
                ErrorMessage = "Расходник с таким наименованием уже существует в выбранной категории!";
                return;
            }

            try
            {
                if (!_isEditMode)
                    _model.CreateConsumable(EditingConsumable.Name, EditingConsumable.ConsumableCategoryId, EditingConsumable.Cost);
                else
                    _model.EditConsumable(EditingConsumable.Id, EditingConsumable.Name,
                        EditingConsumable.ConsumableCategoryId, EditingConsumable.Cost);

                SuccessMessage = _isEditMode ? "Расходник успешно обновлен!" : "Расходник успешно добавлен!";
                ConsumableSaved?.Invoke(this, EventArgs.Empty);

                await System.Threading.Tasks.Task.Delay(800);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
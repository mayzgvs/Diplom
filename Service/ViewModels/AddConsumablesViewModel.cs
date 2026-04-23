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
            set 
            { 
                _editingConsumable = value; 
                OnPropertyChanged(); 
            }
        }

        public ObservableCollection<ConsumablesCategory> ConsumableCategories { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

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

        private void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingConsumable?.Name?.Trim()))
            {
                ErrorMessage = "Введите наименование расходника!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingConsumable.ConsumableCategoryId == 0)
            {
                ErrorMessage = "Выберите категорию расходника!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_model.ConsumableNameExistsInCategory(
                    EditingConsumable.Name.Trim(),
                    EditingConsumable.ConsumableCategoryId,
                    _isEditMode ? EditingConsumable.Id : (int?)null))
            {
                ErrorMessage = "Расходник с таким наименованием уже существует в выбранной категории!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateConsumable(EditingConsumable.Name.Trim(),
                                          EditingConsumable.ConsumableCategoryId,
                                          EditingConsumable.Cost);

                    SuccessMessage = "Расходник успешно добавлен!";
                    MessageBox.Show("Расходник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditConsumable(EditingConsumable.Id,
                                        EditingConsumable.Name.Trim(),
                                        EditingConsumable.ConsumableCategoryId,
                                        EditingConsumable.Cost);

                    SuccessMessage = "Расходник успешно обновлен!";
                    MessageBox.Show("Расходник успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ConsumableSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
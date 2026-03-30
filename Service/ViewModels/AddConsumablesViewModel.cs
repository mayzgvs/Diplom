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

        // Событие для уведомления об успешном сохранении
        public event EventHandler ConsumableSaved;

        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set { _editingConsumable = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConsumablesCategory> ConsumableCategories { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }


        private void LoadCategories()
        {
            ConsumableCategories = new ObservableCollection<ConsumablesCategory>(_model.GetCategories());
        }

        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingConsumable.Name) || EditingConsumable.ConsumableCategoryId == 0)
            {
                MessageBox.Show("Заполните название и категорию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                    _model.CreateConsumable(EditingConsumable.Name, EditingConsumable.ConsumableCategoryId);
                else
                    _model.EditConsumable(EditingConsumable.Id, EditingConsumable.Name, EditingConsumable.ConsumableCategoryId);

                // Вызываем событие перед закрытием окна
                ConsumableSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
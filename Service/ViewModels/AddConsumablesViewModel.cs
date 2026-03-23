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

        public Consumable EditingConsumable
        {
            get => _editingConsumable;
            set { _editingConsumable = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConsumablesCategory> ConsumableCategories { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public AddConsumablesViewModel(Consumable consumable = null)
        {
            ConsumableCategories = new ObservableCollection<ConsumablesCategory>(_model.GetCategories());

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

        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingConsumable.Name) || EditingConsumable.ConsumableCategoryId == 0)
            {
                MessageBox.Show("Заполните название и категорию!");
                return;
            }

            if (!_isEditMode)
                _model.CreateConsumable(EditingConsumable.Name, EditingConsumable.ConsumableCategoryId);
            else
                _model.EditConsumable(EditingConsumable.Id, EditingConsumable.Name, EditingConsumable.ConsumableCategoryId);

            if (parameter is Window window) window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window) window.DialogResult = false;
        }
    }
}
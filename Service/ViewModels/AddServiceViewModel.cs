using Service.Data;
using Service.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ServiceModel = Service.Data.Service;

namespace Service.ViewModels
{
    public class AddServiceViewModel : BaseViewModel
    {
        private readonly ServiceAddEditModel _model = new ServiceAddEditModel();
        private ServiceModel _editingService;
        private bool _isEditMode;

        public event EventHandler ServiceSaved;

        public ServiceModel EditingService
        {
            get => _editingService;
            set { _editingService = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ServiceCategory> ServiceCategories { get; private set; }

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

        private void LoadCategories()
        {
            ServiceCategories = new ObservableCollection<ServiceCategory>(_model.GetCategories());
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingService.Name))
            {
                ErrorMessage = "Заполните название услуги!";
                return;
            }

            if (EditingService.Cost <= 0)
            {
                ErrorMessage = "Ошибка: стоимость должна быть положительным числом!";
                return;
            }

            if (EditingService.ServiceCategoryId == 0)
            {
                ErrorMessage = "Выберите категорию услуги!";
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateService(EditingService.Name, EditingService.Cost, EditingService.ServiceCategoryId);
                    SuccessMessage = "Услуга успешно добавлена!";
                    MessageBox.Show("Услуга успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditService(EditingService.Id, EditingService.Name, EditingService.Cost, EditingService.ServiceCategoryId);
                    SuccessMessage = "Услуга успешно обновлена!";
                    MessageBox.Show("Услуга успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ServiceSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public AddServiceViewModel(ServiceModel service = null)
        {
            LoadCategories();

            if (service == null)
            {
                _isEditMode = false;
                EditingService = new ServiceModel();
            }
            else
            {
                _isEditMode = true;
                EditingService = service;
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
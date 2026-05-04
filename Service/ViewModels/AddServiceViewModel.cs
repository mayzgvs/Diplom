using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        private string _searchCategoryText;
        private ObservableCollection<ServiceCategory> _allCategories;

        public event EventHandler ServiceSaved;

        public ServiceModel EditingService
        {
            get => _editingService;
            set { _editingService = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ServiceCategory> _serviceCategories;
        public ObservableCollection<ServiceCategory> ServiceCategories
        {
            get => _serviceCategories;
            private set
            {
                _serviceCategories = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        private string _errorMessage = "";
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

        public AddServiceViewModel(ServiceModel service = null)
        {
            LoadCategories();

            if (service == null)
            {
                _isEditMode = false;
                EditingService = new ServiceModel { ServiceCategoryId = 0 };
            }
            else
            {
                _isEditMode = true;
                EditingService = service;
                SetSearchTextForEditMode();
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void SetSearchTextForEditMode()
        {
            if (_isEditMode && EditingService?.ServiceCategoryId > 0)
            {
                var category = _allCategories.FirstOrDefault(c => c.Id == EditingService.ServiceCategoryId);
                if (category != null)
                    SearchCategoryText = category.Name;
            }
        }

        private void LoadCategories()
        {
            var categories = _model.GetCategories();
            _allCategories = new ObservableCollection<ServiceCategory>(categories.OrderBy(c => c.Name));
            FilterCategories();
        }

        private void FilterCategories()
        {
            if (string.IsNullOrWhiteSpace(SearchCategoryText))
            {
                ServiceCategories = new ObservableCollection<ServiceCategory>(_allCategories);
            }
            else
            {
                var search = SearchCategoryText.ToLower();
                ServiceCategories = new ObservableCollection<ServiceCategory>(
                    _allCategories.Where(c => c.Name.ToLower().Contains(search)));
            }
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(EditingService?.Name?.Trim()))
            {
                ErrorMessage = "Заполните название услуги!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingService.Cost <= 0)
            {
                ErrorMessage = "Стоимость должна быть больше нуля!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingService.ServiceCategoryId == 0)
            {
                ErrorMessage = "Выберите категорию услуги!";
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateService(EditingService.Name.Trim(), EditingService.Cost, EditingService.ServiceCategoryId);
                    CustomMessageBox.Show("Услуга успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditService(EditingService.Id, EditingService.Name.Trim(), EditingService.Cost, EditingService.ServiceCategoryId);
                    CustomMessageBox.Show("Услуга успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ServiceSaved?.Invoke(this, EventArgs.Empty);

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
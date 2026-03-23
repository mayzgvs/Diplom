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

        public ServiceModel EditingService
        {
            get => _editingService;
            set { _editingService = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ServiceCategory> ServiceCategories { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public AddServiceViewModel(ServiceModel service = null)
        {
            ServiceCategories = new ObservableCollection<ServiceCategory>(_model.GetCategories());

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

        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingService.Name) || EditingService.ServiceCategoryId == 0)
            {
                MessageBox.Show("Заполните название и категорию!");
                return;
            }

            if (!_isEditMode)
                _model.CreateService(EditingService.Name, EditingService.Cost, EditingService.ServiceCategoryId);
            else
                _model.EditService(EditingService.Id, EditingService.Name, EditingService.Cost, EditingService.ServiceCategoryId);

            if (parameter is Window window) window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window) window.DialogResult = false;
        }
    }
}
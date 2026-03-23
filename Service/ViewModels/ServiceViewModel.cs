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
    public class ServiceViewModel : BaseViewModel
    {
        private readonly ServiceItemModel _model = new ServiceItemModel();

        public ObservableCollection<ServiceModel> Services { get; private set; }
        public ObservableCollection<ServiceModel> FilteredServices { get; private set; }

        private ServiceModel _selectedService;
        public ServiceModel SelectedService
        {
            get => _selectedService;
            set { _selectedService = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); Filter(); }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ServiceViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddService());
            EditCommand = new RelayCommand(_ => EditService(), _ => SelectedService != null);
            DeleteCommand = new RelayCommand(_ => DeleteService(), _ => SelectedService != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetServices();
            Services = new ObservableCollection<ServiceModel>(list);
            FilteredServices = new ObservableCollection<ServiceModel>(list);
        }

        private void Filter()
        {
            if (Services == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Services.ToList()
                : Services.Where(s =>
                    s.Name?.ToLower().Contains(SearchText.ToLower()) == true).ToList();

            FilteredServices = new ObservableCollection<ServiceModel>(filtered);
        }

        private void AddService()
        {
            var window = new AddServiceView();
            window.DataContext = new AddServiceViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditService()
        {
            var window = new AddServiceView();
            window.DataContext = new AddServiceViewModel(SelectedService);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteService()
        {
            if (MessageBox.Show($"Удалить услугу '{SelectedService.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteService(SelectedService);
                LoadData();
            }
        }
    }
}
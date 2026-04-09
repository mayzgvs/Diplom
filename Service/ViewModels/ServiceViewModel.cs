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

        private ServiceCategory _selectedCategory;
        public ServiceCategory SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); Filter(); }
        }

        public ObservableCollection<ServiceCategory> Categories { get; private set; }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public ServiceViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddService());
            EditCommand = new RelayCommand(_ => EditService(), _ => SelectedService != null);
            DeleteCommand = new RelayCommand(_ => DeleteService(), _ => SelectedService != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            Services = new ObservableCollection<ServiceModel>();
            FilteredServices = new ObservableCollection<ServiceModel>();
            Categories = new ObservableCollection<ServiceCategory>();
            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetServices();
            var categories = _model.GetCategories();

            Services.Clear();
            foreach (var service in list)
            {
                Services.Add(service);
            }

            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            Filter();

            OnPropertyChanged(nameof(Services));
            OnPropertyChanged(nameof(Categories));
        }

        private void Filter()
        {
            if (Services == null) return;

            var filtered = Services.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var st = SearchText.ToLower();
                filtered = filtered.Where(s => s.Name?.ToLower().Contains(st) == true);
            }

            if (SelectedCategory != null)
                filtered = filtered.Where(s => s.ServiceCategoryId == SelectedCategory.Id);

            FilteredServices.Clear();
            foreach (var service in filtered)
            {
                FilteredServices.Add(service);
            }

            OnPropertyChanged(nameof(FilteredServices));
        }

        private void AddService()
        {
            var window = new AddServiceView();
            var viewModel = new AddServiceViewModel();

            viewModel.ServiceSaved += OnServiceSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void EditService()
        {
            var window = new AddServiceView();
            var viewModel = new AddServiceViewModel(SelectedService);

            viewModel.ServiceSaved += OnServiceSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void OnServiceSaved(object sender, EventArgs e)
        {
            LoadData();

            if (sender is AddServiceViewModel viewModel)
            {
                viewModel.ServiceSaved -= OnServiceSaved;
            }
        }

        private void DeleteService()
        {
            if (SelectedService == null) return;

            if (MessageBox.Show($"Удалить услугу '{SelectedService.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _model.DeleteService(SelectedService);
                    LoadData();
                    MessageBox.Show("Услуга успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
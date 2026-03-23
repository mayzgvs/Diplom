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
    public class CarViewModel : BaseViewModel
    {
        private readonly CarModel _model = new CarModel();

        public ObservableCollection<Car> Cars { get; private set; }
        public ObservableCollection<Car> FilteredCars { get; private set; }

        private Car _selectedCar;
        public Car SelectedCar
        {
            get => _selectedCar;
            set { _selectedCar = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterCars(); }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public CarViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddCar());
            EditCommand = new RelayCommand(_ => EditCar(), _ => SelectedCar != null);
            DeleteCommand = new RelayCommand(_ => DeleteCar(), _ => SelectedCar != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetCars();
            Cars = new ObservableCollection<Car>(list);
            FilteredCars = new ObservableCollection<Car>(list);
        }

        private void FilterCars()
        {
            if (Cars == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Cars.ToList()
                : Cars.Where(c =>
                    (c.Brand?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (c.Model?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (c.RegistrationNumber?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (c.VIN?.ToLower().Contains(SearchText.ToLower()) == true)).ToList();

            FilteredCars = new ObservableCollection<Car>(filtered);
        }

        private void AddCar()
        {
            var window = new AddCarView();
            window.DataContext = new AddCarViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditCar()
        {
            var window = new AddCarView();
            window.DataContext = new AddCarViewModel(SelectedCar);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteCar()
        {
            if (MessageBox.Show($"Удалить автомобиль {SelectedCar.Brand} {SelectedCar.Model}?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteCar(SelectedCar);
                LoadData();
            }
        }
    }
}
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
            set
            {
                _selectedCar = value;
                OnPropertyChanged();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterCars();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public CarViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddCar());
            EditCommand = new RelayCommand(_ => EditCar(), _ => SelectedCar != null);
            DeleteCommand = new RelayCommand(_ => DeleteCar(), _ => SelectedCar != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = "");

            Cars = new ObservableCollection<Car>();
            FilteredCars = new ObservableCollection<Car>();
            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetCars().OrderBy(c => c.Brand).ThenBy(c => c.Model).ThenBy(c => c.RegistrationNumber);

            Cars.Clear();
            FilteredCars.Clear();

            foreach (var car in list)
            {
                Cars.Add(car);
                FilteredCars.Add(car);
            }

            OnPropertyChanged(nameof(Cars));
            OnPropertyChanged(nameof(FilteredCars));
        }

        private void FilterCars()
        {
            if (Cars == null) return;

            var search = (SearchText ?? "").Trim().ToLower();

            var filtered = string.IsNullOrEmpty(search)
                ? Cars.ToList()
                : Cars.Where(c =>
                    (c.Brand?.ToLower().Contains(search) == true) ||
                    (c.Model?.ToLower().Contains(search) == true) ||
                    (c.RegistrationNumber?.ToLower().Contains(search) == true) ||
                    (c.VIN?.ToLower().Contains(search) == true)
                  ).ToList();

            FilteredCars.Clear();
            foreach (var car in filtered)
            {
                FilteredCars.Add(car);
            }
        }

        private void AddCar()
        {
            var window = new AddCarView();
            var viewModel = new AddCarViewModel();

            viewModel.CarSaved += OnCarSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void EditCar()
        {
            var window = new AddCarView();
            var viewModel = new AddCarViewModel(SelectedCar);

            viewModel.CarSaved += OnCarSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void OnCarSaved(object sender, EventArgs e)
        {
            LoadData();
            FilterCars();

            if (sender is AddCarViewModel viewModel)
            {
                viewModel.CarSaved -= OnCarSaved;
            }
        }

        private void DeleteCar()
        {
            if (SelectedCar == null) return;

            // Сохраняем информацию об автомобиле до удаления
            var carToDelete = SelectedCar;
            var carBrand = carToDelete.Brand;
            var carModel = carToDelete.Model;
            var carRegistration = carToDelete.RegistrationNumber;

            // Получаем информацию о связанных заявках до удаления
            var requestsCount = _model.GetRepairRequestsByCarId(carToDelete.Id).Count;

            string warningMessage;
            MessageBoxImage icon = MessageBoxImage.Question;

            if (requestsCount > 0)
            {
                warningMessage = $"Вы действительно хотите удалить автомобиль:\n" +
                                $"«{carBrand} {carModel}»\n" +
                                $"Госномер: {carRegistration}\n\n" +
                                $"ВНИМАНИЕ! Это приведет к КАСКАДНОМУ удалению:\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                $"• {requestsCount} заявок на ремонт\n" +
                                $"• Всех связанных работ и материалов\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                $"Данное действие НЕОБРАТИМО!\n\n" +
                                $"Вы уверены, что хотите продолжить?";
                icon = MessageBoxImage.Warning;
            }
            else
            {
                warningMessage = $"Удалить автомобиль {carBrand} {carModel}?\n" +
                                $"Госномер: {carRegistration}";
            }

            var result = CustomMessageBox.Show(warningMessage,
                requestsCount > 0 ? "Подтверждение каскадного удаления" : "Подтверждение удаления",
                MessageBoxButton.YesNo, icon);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Выполняем удаление
                    _model.DeleteCar(carToDelete);

                    // Очищаем выбранный элемент
                    SelectedCar = null;

                    // Обновляем списки
                    LoadData();
                    FilterCars();

                    // Показываем сообщение об успехе
                    var message = requestsCount > 0
                        ? $"✓ Автомобиль и все связанные данные успешно удалены!\n\n" +
                          $"Удалено:\n" +
                          $"• Автомобиль: {carBrand} {carModel}\n" +
                          $"• Заявок: {requestsCount}\n" +
                          $"• Все связанные работы"
                        : $"✓ Автомобиль {carBrand} {carModel} успешно удален!";

                    CustomMessageBox.Show(message, "Каскадное удаление выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при удалении: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
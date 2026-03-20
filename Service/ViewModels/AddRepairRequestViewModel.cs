using Service.Data;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddRepairRequestViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private RepairRequest _editingRepairRequest;
        private bool _isEditMode;
        private ObservableCollection<Car> _cars;
        private ObservableCollection<StatusRequest> _statuses;

        public RepairRequest EditingRepairRequest
        {
            get => _editingRepairRequest;
            set
            {
                _editingRepairRequest = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Car> Cars
        {
            get => _cars;
            set
            {
                _cars = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<StatusRequest> Statuses
        {
            get => _statuses;
            set
            {
                _statuses = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }
        public ICommand AddNewCarCommand { get; set; }

        public AddRepairRequestViewModel(ApplicationContext context, RepairRequest request = null)
        {
            _context = context;

            // Загружаем списки
            Cars = new ObservableCollection<Car>(_context.Cars.Include(c => c.Client).ToList());
            Statuses = new ObservableCollection<StatusRequest>(_context.StatusRequests.ToList());

            if (request == null)
            {
                _isEditMode = false;
                EditingRepairRequest = new RepairRequest
                {
                    StartDate = DateTime.Now,
                    StatusId = Statuses.FirstOrDefault()?.Id ?? 1
                };
            }
            else
            {
                _isEditMode = true;
                EditingRepairRequest = new RepairRequest
                {
                    Id = request.Id,
                    CarId = request.CarId,
                    Client = request.Client,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalCost = request.TotalCost,
                    StatusId = request.StatusId,
                    Car = request.Car,
                    Status = request.Status
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddNewCarCommand = new RelayCommand(AddNewCar);
        }

        private void AddNewCar(object parameter)
        {
            var addCarWindow = new AddCarView();
            var addCarViewModel = new AddCarViewModel(_context);
            addCarWindow.DataContext = addCarViewModel;

            if (addCarWindow.ShowDialog() == true)
            {
                // Обновляем список автомобилей
                Cars = new ObservableCollection<Car>(_context.Cars.Include(c => c.Client).ToList());

                // Автоматически выбираем новый автомобиль
                if (Cars.Count > 0)
                {
                    EditingRepairRequest.Car = Cars.LastOrDefault();
                    EditingRepairRequest.CarId = EditingRepairRequest.Car?.Id ?? 0;

                    // Автоматически подставляем клиента
                    if (EditingRepairRequest.Car?.Client != null)
                    {
                        EditingRepairRequest.Client = EditingRepairRequest.Car.Client.FullName;
                    }
                }
            }
        }

        private void Save(object parameter)
        {
            try
            {
                if (EditingRepairRequest.CarId == 0)
                {
                    MessageBox.Show("Выберите автомобиль.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingRepairRequest.Client))
                {
                    MessageBox.Show("Клиент должен быть указан.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingRepairRequest.StartDate == DateTime.MinValue)
                {
                    MessageBox.Show("Укажите дату начала работ.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingRepairRequest.StatusId == 0)
                {
                    MessageBox.Show("Выберите статус заявки.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_isEditMode)
                {
                    _context.RepairRequests.Add(EditingRepairRequest);
                }
                else
                {
                    var existing = _context.RepairRequests.Find(EditingRepairRequest.Id);
                    if (existing != null)
                    {
                        existing.CarId = EditingRepairRequest.CarId;
                        existing.Client = EditingRepairRequest.Client;
                        existing.StartDate = EditingRepairRequest.StartDate;
                        existing.EndDate = EditingRepairRequest.EndDate;
                        existing.TotalCost = EditingRepairRequest.TotalCost;
                        existing.StatusId = EditingRepairRequest.StatusId;
                    }
                }

                _context.SaveChanges();

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
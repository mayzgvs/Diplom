using Service.Data;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DataService = Service.Data.Service; // Добавляем алиас

namespace Service.ViewModels
{
    public class AddServiceViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private DataService _editingService; // Используем алиас
        private bool _isEditMode;
        private ObservableCollection<ServiceCategory> _serviceCategories;

        public DataService EditingService // Используем алиас
        {
            get => _editingService;
            set
            {
                _editingService = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ServiceCategory> ServiceCategories
        {
            get => _serviceCategories;
            set
            {
                _serviceCategories = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }
        public ICommand AddCategoryCommand { get; set; }

        public AddServiceViewModel(ApplicationContext context, DataService service = null) // Используем алиас
        {
            _context = context;

            ServiceCategories = new ObservableCollection<ServiceCategory>(_context.ServiceCategories.ToList());

            if (service == null)
            {
                _isEditMode = false;
                EditingService = new DataService(); // Используем алиас
            }
            else
            {
                _isEditMode = true;
                EditingService = new DataService // Используем алиас
                {
                    Id = service.Id,
                    Name = service.Name,
                    Cost = service.Cost,
                    ServiceCategoryId = service.ServiceCategoryId,
                    ServiceCategory = service.ServiceCategory
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
            AddCategoryCommand = new RelayCommand(AddNewCategory);
        }

        private void AddNewCategory(object parameter)
        {
            MessageBox.Show("Добавление новой категории будет доступно в следующей версии.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditingService.Name))
                {
                    MessageBox.Show("Название услуги обязательно для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingService.Cost < 0)
                {
                    MessageBox.Show("Стоимость не может быть отрицательной.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EditingService.ServiceCategoryId == 0)
                {
                    MessageBox.Show("Выберите категорию услуги.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_isEditMode)
                {
                    _context.Services.Add(EditingService);
                }
                else
                {
                    var existing = _context.Services.Find(EditingService.Id);
                    if (existing != null)
                    {
                        existing.Name = EditingService.Name;
                        existing.Cost = EditingService.Cost;
                        existing.ServiceCategoryId = EditingService.ServiceCategoryId;
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
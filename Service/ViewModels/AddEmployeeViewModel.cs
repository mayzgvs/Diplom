using Service.Data;
using System;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddEmployeeViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private Employee _editingEmployee;
        private bool _isEditMode;

        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set
            {
                _editingEmployee = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }

        public AddEmployeeViewModel(ApplicationContext context, Employee employee = null)
        {
            _context = context;

            if (employee == null)
            {
                _isEditMode = false;
                EditingEmployee = new Employee();
            }
            else
            {
                _isEditMode = true;
                EditingEmployee = new Employee
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    ContactNumber = employee.ContactNumber
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void Save(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditingEmployee.LastName))
                {
                    MessageBox.Show("Фамилия обязательна для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
                {
                    MessageBox.Show("Имя обязательно для заполнения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_isEditMode)
                {
                    _context.Employees.Add(EditingEmployee);
                }
                else
                {
                    var existing = _context.Employees.Find(EditingEmployee.Id);
                    if (existing != null)
                    {
                        existing.FirstName = EditingEmployee.FirstName;
                        existing.LastName = EditingEmployee.LastName;
                        existing.ContactNumber = EditingEmployee.ContactNumber;
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
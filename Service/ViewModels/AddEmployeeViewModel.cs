using Service.Data;
using Service.Models;
using Service.Utility;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddEmployeeViewModel : BaseViewModel
    {
        private readonly EmployeeAddEditModel _model = new EmployeeAddEditModel();
        private Employee _editingEmployee;
        private bool _isEditMode;

        public event EventHandler EmployeeSaved;

        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set { _editingEmployee = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }


        private void Save(object parameter)
        {
            if (string.IsNullOrWhiteSpace(EditingEmployee.LastName))
            {
                MessageBox.Show("Введите фамилию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
            {
                MessageBox.Show("Введите имя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingEmployee.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingEmployee.ContactNumber))
                {
                    MessageBox.Show("Некорректный формат номера телефона!\nФормат: +7XXXXXXXXXX (10 цифр после +7)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.PhoneExists(EditingEmployee.ContactNumber, _isEditMode ? EditingEmployee.Id : (int?)null))
                {
                    MessageBox.Show("Сотрудник с таким номером телефона уже существует!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (!_isEditMode)
                    _model.CreateEmployee(EditingEmployee.FirstName, EditingEmployee.LastName,
                        EditingEmployee.ContactNumber);
                else
                    _model.EditEmployee(EditingEmployee.Id, EditingEmployee.FirstName, EditingEmployee.LastName,
                        EditingEmployee.ContactNumber);

                EmployeeSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public AddEmployeeViewModel(Employee employee = null)
        {
            if (employee == null)
            {
                _isEditMode = false;
                EditingEmployee = new Employee();
            }
            else
            {
                _isEditMode = true;
                EditingEmployee = employee;
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
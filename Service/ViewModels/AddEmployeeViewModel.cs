using Service.Data;
using Service.Models;
using Service.Utility;
using System;
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

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        private async void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingEmployee.LastName))
            {
                ErrorMessage = "Введите фамилию!";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
            {
                ErrorMessage = "Введите имя!";
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingEmployee.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingEmployee.ContactNumber))
                {
                    ErrorMessage = "Некорректный формат номера телефона!\nФормат: +7XXXXXXXXXX (10 цифр после +7)";
                    return;
                }

                if (_model.PhoneExists(EditingEmployee.ContactNumber, _isEditMode ? EditingEmployee.Id : (int?)null))
                {
                    ErrorMessage = "Сотрудник с таким номером телефона уже существует!";
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

                SuccessMessage = _isEditMode ? "Сотрудник успешно обновлен!" : "Сотрудник успешно добавлен!";
                EmployeeSaved?.Invoke(this, EventArgs.Empty);

                await System.Threading.Tasks.Task.Delay(800);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
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
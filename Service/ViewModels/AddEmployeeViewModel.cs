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
            set 
            { 
                _editingEmployee = value; 
                OnPropertyChanged(); 
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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

        private void Save(object parameter)
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(EditingEmployee.LastName) ||
                string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
            {
                ErrorMessage = "Введите фамилию и имя сотрудника!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingEmployee.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingEmployee.ContactNumber))
                {
                    ErrorMessage = "Некорректный формат номера телефона!\nПример: +7XXXXXXXXXX";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.PhoneExists(EditingEmployee.ContactNumber, _isEditMode ? EditingEmployee.Id : (int?)null))
                {
                    ErrorMessage = "Сотрудник с таким номером телефона уже существует!";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateEmployee(EditingEmployee.FirstName, EditingEmployee.LastName, EditingEmployee.ContactNumber);
                    MessageBox.Show("Сотрудник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditEmployee(EditingEmployee.Id, EditingEmployee.FirstName, EditingEmployee.LastName, EditingEmployee.ContactNumber);
                    MessageBox.Show("Сотрудник успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                EmployeeSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
using Service.Data;
using Service.Models;
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

        // Событие для уведомления об успешном сохранении
        public event EventHandler EmployeeSaved;

        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set { _editingEmployee = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

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
            if (string.IsNullOrWhiteSpace(EditingEmployee.LastName) || string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
            {
                MessageBox.Show("Фамилия и имя обязательны!");
                return;
            }

            try
            {
                if (!_isEditMode)
                    _model.CreateEmployee(EditingEmployee.FirstName, EditingEmployee.LastName, EditingEmployee.ContactNumber);
                else
                    _model.EditEmployee(EditingEmployee.Id, EditingEmployee.FirstName, EditingEmployee.LastName, EditingEmployee.ContactNumber);

                // Вызываем событие перед закрытием окна
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

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
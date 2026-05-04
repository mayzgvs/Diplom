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
    public class EmployeeViewModel : BaseViewModel
    {
        private readonly EmployeeModel _model = new EmployeeModel();

        public ObservableCollection<Employee> Employees { get; private set; }
        public ObservableCollection<Employee> FilteredEmployees { get; private set; }

        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterEmployees();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public EmployeeViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddEmployee());
            EditCommand = new RelayCommand(_ => EditEmployee(), _ => SelectedEmployee != null);
            DeleteCommand = new RelayCommand(_ => DeleteEmployee(), _ => SelectedEmployee != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = "");

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetEmployees().OrderBy(e => e.LastName).ThenBy(e => e.FirstName);
            Employees = new ObservableCollection<Employee>(list);
            FilteredEmployees = new ObservableCollection<Employee>(list);
            OnPropertyChanged(nameof(Employees));
            OnPropertyChanged(nameof(FilteredEmployees));
        }

        private void FilterEmployees()
        {
            if (Employees == null) return;

            var search = (SearchText ?? "").Trim().ToLower();

            var filtered = string.IsNullOrEmpty(search)
                ? Employees.ToList()
                : Employees.Where(e =>
                    (e.LastName?.ToLower().Contains(search) == true) ||
                    (e.FirstName?.ToLower().Contains(search) == true) ||
                    (e.ContactNumber?.ToLower().Contains(search) == true)
                  ).ToList();

            FilteredEmployees.Clear();
            foreach (var employee in filtered)
            {
                FilteredEmployees.Add(employee);
            }
        }

        private void AddEmployee()
        {
            var window = new AddEmployeeView();
            var viewModel = new AddEmployeeViewModel();

            viewModel.EmployeeSaved += OnEmployeeSaved;

            window.DataContext = viewModel;
            window.ShowDialog();
        }

        private void EditEmployee()
        {
            var window = new AddEmployeeView();
            var viewModel = new AddEmployeeViewModel(SelectedEmployee);

            viewModel.EmployeeSaved += OnEmployeeSaved;

            window.DataContext = viewModel;
            window.ShowDialog();
        }

        private void OnEmployeeSaved(object sender, EventArgs e)
        {
            LoadData();

            if (sender is AddEmployeeViewModel viewModel)
            {
                viewModel.EmployeeSaved -= OnEmployeeSaved;
            }
        }

        private void DeleteEmployee()
        {
            if (SelectedEmployee == null) return;

            var result = CustomMessageBox.Show($"Удалить сотрудника {SelectedEmployee.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _model.DeleteEmployee(SelectedEmployee);
                    LoadData();
                    CustomMessageBox.Show("Сотрудник успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
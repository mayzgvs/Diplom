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
            set { _searchText = value; OnPropertyChanged(); FilterEmployees(); }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public EmployeeViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddEmployee());
            EditCommand = new RelayCommand(_ => EditEmployee(), _ => SelectedEmployee != null);
            DeleteCommand = new RelayCommand(_ => DeleteEmployee(), _ => SelectedEmployee != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetEmployees();
            Employees = new ObservableCollection<Employee>(list);
            FilteredEmployees = new ObservableCollection<Employee>(list);
        }

        private void FilterEmployees()
        {
            if (Employees == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Employees.ToList()
                : Employees.Where(e =>
                    (e.LastName?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (e.FirstName?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (e.ContactNumber?.ToLower().Contains(SearchText.ToLower()) == true)).ToList();

            FilteredEmployees = new ObservableCollection<Employee>(filtered);
        }

        private void AddEmployee()
        {
            var window = new AddEmployeeView();
            window.DataContext = new AddEmployeeViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditEmployee()
        {
            var window = new AddEmployeeView();
            window.DataContext = new AddEmployeeViewModel(SelectedEmployee);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteEmployee()
        {
            if (MessageBox.Show($"Удалить сотрудника {SelectedEmployee.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteEmployee(SelectedEmployee);
                LoadData();
            }
        }
    }
}
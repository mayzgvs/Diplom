using Service.Data;
using Service.Views;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Service.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private readonly DispatcherTimer _timer;

        // Текущая дата и время
        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        private string _currentTime;
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        // Статистика
        private int _activeRequests;
        public int ActiveRequests
        {
            get => _activeRequests;
            set
            {
                _activeRequests = value;
                OnPropertyChanged();
            }
        }

        private int _totalClients;
        public int TotalClients
        {
            get => _totalClients;
            set
            {
                _totalClients = value;
                OnPropertyChanged();
            }
        }

        private decimal _monthlyRevenue;
        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            set
            {
                _monthlyRevenue = value;
                OnPropertyChanged();
            }
        }

        // Текущая страница
        private object _currentPage;
        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        // Команда навигации
        public ICommand NavigateCommand { get; }

        public MainWindowViewModel()
        {
            _context = new ApplicationContext();

            UpdateDateTime();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            NavigateCommand = new RelayCommand(Navigate);

            LoadStatisticsAsync();

            CurrentPage = GetPageInstance("Clients");
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentDate = now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
            CurrentTime = now.ToString("HH:mm");
        }

        private void Navigate(object parameter)
        {
            if (parameter is string pageName)
            {
                CurrentPage = GetPageInstance(pageName);
            }
        }

        private object GetPageInstance(string pageName)
        {
            switch (pageName)
            {
                case "Clients":
                    return new ClientView { DataContext = new ClientViewModel() };
                case "Cars":
                    return new CarView { DataContext = new CarViewModel() };
                case "RepairRequests": 
                    return new RepairView { DataContext = new RepairRequestViewModel() };
                case "Services":
                    return new ServiceView { DataContext = new ServiceViewModel() };
                case "Consumables":
                    return new ConsumablesView { DataContext = new ConsumableViewModel() };
                case "Employees":
                    return new EmployeeView { DataContext = new EmployeeViewModel() };
                case "Reports":
                    return new ReportView { DataContext = new ReportsViewModel() };
                default:
                    return new TextBlock { Text = "Страница не найдена" };
            }
        }

        private async void LoadStatisticsAsync()
        {
            try
            {
                // Активные заявки (статус 1 - Новая, 2 - В работе)
                ActiveRequests = await _context.RepairRequests
                    .CountAsync(r => r.StatusId == 1 || r.StatusId == 2);

                // Общее количество клиентов
                TotalClients = await _context.Clients.CountAsync();

                // Выручка за текущий месяц
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                MonthlyRevenue = await _context.RepairRequests
                    .Where(r => r.StatusId == 3 && 
                           r.EndDate >= startOfMonth &&
                           r.EndDate <= endOfMonth)
                    .SumAsync(r => (decimal?)r.TotalCost) ?? 0;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RefreshStatistics()
        {
            LoadStatisticsAsync();
        }
    }
}
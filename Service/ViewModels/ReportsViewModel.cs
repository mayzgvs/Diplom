using Service.Models;
using System;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly StatisticsModel _statistics = new StatisticsModel();

        // Статистика
        public int ActiveRequests => _statistics.GetActiveRequestsCount();
        public int TotalClients => _statistics.GetTotalClientsCount();
        public decimal MonthlyRevenue => _statistics.GetMonthlyRevenue();
        public int CompletedRequests => _statistics.GetCompletedRequestsCount();
        public int ActiveEmployees => _statistics.GetActiveEmployeesCount();

        public (decimal Avg, decimal Min, decimal Max) ServiceCostStats => _statistics.GetServiceCostStats();

        public ICommand LoadedCommand { get; }

        public ReportsViewModel()
        {
            LoadedCommand = new RelayCommand(_ => RefreshStats());
        }

        private void RefreshStats()
        {
            OnPropertyChanged(nameof(ActiveRequests));
            OnPropertyChanged(nameof(TotalClients));
            OnPropertyChanged(nameof(MonthlyRevenue));
            OnPropertyChanged(nameof(CompletedRequests));
            OnPropertyChanged(nameof(ActiveEmployees));
            OnPropertyChanged(nameof(ServiceCostStats));
        }
    }
}
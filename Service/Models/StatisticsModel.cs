using Service.Data;
using Service.Utility;
using System;

namespace Service.Models
{
    public class StatisticsModel
    {
        private readonly ClientModel _clientModel = new ClientModel();
        private readonly EmployeeModel _employeeModel = new EmployeeModel();
        private readonly ServiceItemModel _serviceModel = new ServiceItemModel();

        public int GetActiveRequestsCount() => DbManager.GetActiveRequestsCount();
        public int GetTotalClientsCount() => _clientModel.GetClients().Count;
        public int GetCompletedRequestsCount() => DbManager.GetCompletedRequestsCount();
        public int GetActiveEmployeesCount() => _employeeModel.GetActiveEmployeesCount();

        public decimal GetMonthlyRevenue()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return DbManager.GetRevenueForPeriod(start, end);
        }

        public (decimal Avg, decimal Min, decimal Max) GetServiceCostStats()
        {
            return (
                _serviceModel.GetAverageCost(),
                _serviceModel.GetMinCost(),
                _serviceModel.GetMaxCost()
            );
        }

        public decimal GetRevenueForPeriod(DateTime startDate, DateTime endDate)
        {
            return DbManager.GetRevenueForPeriod(startDate, endDate);
        }
    }
}
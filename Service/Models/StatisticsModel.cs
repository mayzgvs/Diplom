using Service.Utility;
using System;
using System.Linq;

namespace Service.Models
{
    public class StatisticsModel
    {
        // Используем другие модели вместо прямых вызовов DbManager
        private readonly RepairRequestModel _requestModel;
        private readonly ClientModel _clientModel;
        private readonly EmployeeModel _employeeModel;
        private readonly ServiceItemModel _serviceModel;

        public StatisticsModel()
        {
            _requestModel = new RepairRequestModel();
            _clientModel = new ClientModel();
            _employeeModel = new EmployeeModel();
            _serviceModel = new ServiceItemModel();
        }

        public int GetActiveRequestsCount()
        {
            return DbManager.GetActiveRequestsCount();
        }

        public int GetTotalClientsCount()
        {
            return _clientModel.GetClients().Count;
        }

        public decimal GetMonthlyRevenue()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            return DbManager.GetRevenueForPeriod(startOfMonth, endOfMonth);
        }

        public int GetCompletedRequestsCount()
        {
            return DbManager.GetCompletedRequestsCount();
        }

        public int GetActiveEmployeesCount()
        {
            return _employeeModel.GetActiveEmployeesCount();
        }

        public (decimal Avg, decimal Min, decimal Max) GetServiceCostStats()
        {
            return (
                _serviceModel.GetAverageCost(),
                _serviceModel.GetMinCost(),
                _serviceModel.GetMaxCost()
            );
        }
    }
}
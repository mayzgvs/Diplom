// ReportModels.cs - новые модели для отчетов
using System;
using System.Collections.Generic;

namespace Service.Models
{
    public class RevenueReport
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerDay { get; set; }
        public int TotalRequests { get; set; }
        public decimal AverageRequestValue { get; set; }
        public Dictionary<string, decimal> RevenueByDay { get; set; }
        public Dictionary<string, decimal> RevenueByServiceType { get; set; }
        public Dictionary<string, int> RequestsByStatus { get; set; }
        public List<TopServiceItem> TopServices { get; set; }
        public List<TopEmployeeItem> TopEmployees { get; set; }
        public List<TopClientItem> TopClients { get; set; }

        public RevenueReport()
        {
            RevenueByDay = new Dictionary<string, decimal>();
            RevenueByServiceType = new Dictionary<string, decimal>();
            RequestsByStatus = new Dictionary<string, int>();
            TopServices = new List<TopServiceItem>();
            TopEmployees = new List<TopEmployeeItem>();
            TopClients = new List<TopClientItem>();
        }
    }

    public class TopServiceItem
    {
        public string ServiceName { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
        public string Category { get; set; }
        public int Index { get; set; }
    }

    public class TopEmployeeItem
    {
        public string EmployeeName { get; set; }
        public int CompletedJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageJobValue { get; set; }
        public int Index { get; set; }
    }

    public class TopClientItem
    {
        public string ClientName { get; set; }
        public int RequestsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string ContactNumber { get; set; }
        public int Index { get; set; }
    }

    public class DetailedReport
    {
        public DateTime GeneratedDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<ReportRequestItem> Requests { get; set; }
        public ReportSummary Summary { get; set; }

        public DetailedReport()
        {
            Requests = new List<ReportRequestItem>();
            Summary = new ReportSummary();
        }
    }

    public class ReportRequestItem
    {
        public int RequestId { get; set; }
        public string ClientName { get; set; }
        public string CarInfo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public decimal TotalCost { get; set; }
        public List<ReportWorkItem> WorkItems { get; set; }

        public ReportRequestItem()
        {
            WorkItems = new List<ReportWorkItem>();
        }
    }

    public class ReportWorkItem
    {
        public string ServiceName { get; set; }
        public string EmployeeName { get; set; }
        public decimal Cost { get; set; }
    }

    public class ReportSummary
    {
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CancelledRequests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRequestValue { get; set; }
        public int UniqueClients { get; set; }
        public int UniqueCars { get; set; }
        public int TotalWorkItems { get; set; }
        public int ActiveEmployees { get; set; }
    }
}
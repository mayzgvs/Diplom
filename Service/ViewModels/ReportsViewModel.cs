// ReportsViewModel.cs — ПОЛНАЯ ВЕРСИЯ С ВСЕМИ МЕТОДАМИ ЭКСПОРТА
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Service.Data;
using Service.Models;
using Service.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Word = Microsoft.Office.Interop.Word;

namespace Service.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        private RevenueReport _currentRevenueReport;
        public RevenueReport CurrentRevenueReport
        {
            get => _currentRevenueReport;
            set { _currentRevenueReport = value; OnPropertyChanged(); }
        }

        private DetailedReport _currentDetailedReport;
        public DetailedReport CurrentDetailedReport
        {
            get => _currentDetailedReport;
            set { _currentDetailedReport = value; OnPropertyChanged(); }
        }

        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PeriodDisplay));
                LoadReports();
            }
        }

        private DateTime _endDate = DateTime.Now.Date;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PeriodDisplay));
                LoadReports();
            }
        }

        private string _periodPreset = "месяц";
        public string PeriodPreset
        {
            get => _periodPreset;
            set
            {
                _periodPreset = value;
                ApplyPeriodPreset();
                OnPropertyChanged();
            }
        }

        public List<string> PeriodPresets { get; private set; }

        public string PeriodDisplay => $"{StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterDetailedReport();
            }
        }

        private string _statusFilter = "Все";
        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                FilterDetailedReport();
            }
        }

        public ObservableCollection<string> StatusFilterOptions { get; private set; }
        public ObservableCollection<ReportRequestItem> FilteredRequests { get; private set; }

        private List<ReportRequestItem> _allRequests = new List<ReportRequestItem>();
        private ReportRequestItem _selectedRequest;

        public ReportRequestItem SelectedRequest
        {
            get => _selectedRequest;
            set
            {
                _selectedRequest = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanExportSingleRequest));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CanExportSingleRequest => SelectedRequest != null;

        // Команды
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportRevenueToExcelCommand { get; private set; }
        public ICommand ExportRevenueToWordCommand { get; private set; }
        public ICommand ExportDetailedToExcelCommand { get; private set; }
        public ICommand ExportDetailedToWordCommand { get; private set; }
        public ICommand ExportSingleRequestToExcelCommand { get; private set; }
        public ICommand ExportSingleRequestToWordCommand { get; private set; }

        public ReportsViewModel()
        {
            _context = new ApplicationContext();

            CurrentRevenueReport = new RevenueReport();
            CurrentDetailedReport = new DetailedReport();

            PeriodPresets = new List<string> { "день", "неделя", "месяц", "полгода", "год" };
            StatusFilterOptions = new ObservableCollection<string> { "Все", "Новые", "В работе", "Завершённые", "Отменённые" };
            FilteredRequests = new ObservableCollection<ReportRequestItem>();

            InitializeCommands();
            LoadReports();
        }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(_ => LoadReports());
            ExportRevenueToExcelCommand = new RelayCommand(_ => ExportRevenueReportToExcel());
            ExportRevenueToWordCommand = new RelayCommand(_ => ExportRevenueReportToWord());
            ExportDetailedToExcelCommand = new RelayCommand(_ => ExportDetailedReportToExcel());
            ExportDetailedToWordCommand = new RelayCommand(_ => ExportDetailedReportToWord());
            ExportSingleRequestToExcelCommand = new RelayCommand(_ => ExportSingleRequestToExcel(), _ => CanExportSingleRequest);
            ExportSingleRequestToWordCommand = new RelayCommand(_ => ExportSingleRequestToWord(), _ => CanExportSingleRequest);
        }

        private void ApplyPeriodPreset()
        {
            var today = DateTime.Today;

            switch (PeriodPreset)
            {
                case "день":
                    StartDate = today;
                    EndDate = today;
                    break;

                case "неделя":
                    int dayOfWeek = (int)today.DayOfWeek;
                    int daysToMonday = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
                    StartDate = today.AddDays(daysToMonday);
                    EndDate = StartDate.AddDays(6);
                    break;

                case "месяц":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = StartDate.AddMonths(1).AddDays(-1);
                    break;

                case "полгода":
                    StartDate = today.AddMonths(-6);
                    EndDate = today;
                    break;

                case "год":
                    StartDate = new DateTime(today.Year, 1, 1);
                    EndDate = new DateTime(today.Year, 12, 31);
                    break;
            }

            LoadReports();
        }

        private void LoadReports()
        {
            LoadRevenueReport();
            LoadDetailedReport();
        }

        // ====================== ЗАГРУЗКА ОТЧЁТОВ ======================
        private void LoadRevenueReport()
        {
            try
            {
                var report = new RevenueReport();
                var requests = DbManager.GetRequestsByDateRange(StartDate, EndDate);

                if (requests == null || !requests.Any())
                {
                    CurrentRevenueReport = report;
                    return;
                }

                report.TotalRequests = requests.Count;
                var completedRequests = requests.Where(r => r.StatusId == 3).ToList();
                report.TotalRevenue = completedRequests.Sum(r => r.TotalCost);
                report.AverageRequestValue = report.TotalRequests > 0 ? report.TotalRevenue / report.TotalRequests : 0;
                var daysInPeriod = (EndDate - StartDate).Days + 1;
                report.AverageRevenuePerDay = daysInPeriod > 0 ? report.TotalRevenue / daysInPeriod : 0;

                report.RevenueByDay = new Dictionary<string, decimal>();
                for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
                {
                    var dayRevenue = completedRequests.Where(r => r.StartDate.Date == date.Date).Sum(r => r.TotalCost);
                    report.RevenueByDay[date.ToString("dd.MM.yyyy")] = dayRevenue;
                }

                report.RequestsByStatus = requests
                    .GroupBy(r => r.Status?.Name ?? "Неизвестно")
                    .ToDictionary(g => g.Key, g => g.Count());

                var allWorkItems = new List<WorkItem>();
                foreach (var req in requests)
                {
                    var workItems = DbManager.GetWorkItemsByRequestId(req.Id);
                    if (workItems != null) allWorkItems.AddRange(workItems);
                }

                report.TopServices = allWorkItems
                    .Where(wi => wi.ServiceId.HasValue)
                    .GroupBy(wi => wi.ServiceId.Value)
                    .Select(g => new TopServiceItem
                    {
                        ServiceName = GetServiceNameById(g.Key),
                        Count = g.Count(),
                        Revenue = g.Sum(wi => wi.Cost)
                    })
                    .OrderByDescending(t => t.Revenue)
                    .Take(5)
                    .ToList();

                for (int i = 0; i < report.TopServices.Count; i++)
                    report.TopServices[i].Index = i + 1;

                report.TopEmployees = allWorkItems
                    .Where(wi => wi.EmployeeId.HasValue)
                    .GroupBy(wi => wi.EmployeeId.Value)
                    .Select(g => new TopEmployeeItem
                    {
                        EmployeeName = GetEmployeeNameById(g.Key),
                        CompletedJobs = g.Count(),
                        TotalRevenue = g.Sum(wi => wi.Cost),
                        AverageJobValue = g.Average(wi => wi.Cost)
                    })
                    .OrderByDescending(t => t.TotalRevenue)
                    .Take(5)
                    .ToList();

                for (int i = 0; i < report.TopEmployees.Count; i++)
                    report.TopEmployees[i].Index = i + 1;

                report.TopClients = requests
                    .Where(r => r.Car?.OwnerId != null)
                    .GroupBy(r => r.Car.OwnerId)
                    .Select(g => new TopClientItem
                    {
                        ClientName = GetClientNameById(g.Key),
                        RequestsCount = g.Count(),
                        TotalSpent = g.Where(r => r.StatusId == 3).Sum(r => r.TotalCost),
                        ContactNumber = GetClientPhoneById(g.Key)
                    })
                    .OrderByDescending(t => t.TotalSpent)
                    .Take(5)
                    .ToList();

                for (int i = 0; i < report.TopClients.Count; i++)
                    report.TopClients[i].Index = i + 1;

                CurrentRevenueReport = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта по выручке: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetServiceNameById(int serviceId) => DbManager.GetServiceById(serviceId)?.Name ?? "Неизвестно";
        private string GetEmployeeNameById(int employeeId) => DbManager.GetEmployeeById(employeeId)?.FullName ?? "Неизвестно";
        private string GetClientNameById(int clientId) => DbManager.GetClientById(clientId)?.FullName ?? "Неизвестно";
        private string GetClientPhoneById(int clientId) => DbManager.GetClientById(clientId)?.ContactNumber ?? "";

        private void LoadDetailedReport()
        {
            try
            {
                var report = new DetailedReport
                {
                    GeneratedDate = DateTime.Now,
                    PeriodStart = StartDate,
                    PeriodEnd = EndDate
                };

                var requests = DbManager.GetRequestsByDateRange(StartDate, EndDate);
                _allRequests.Clear();

                if (requests != null)
                {
                    foreach (var req in requests)
                    {
                        var car = req.Car;
                        var client = car?.Client;
                        string carInfo = car != null ? $"{car.Brand} {car.Model} ({car.RegistrationNumber})" : "Не указан";

                        string statusName = (req.Status?.Name ?? DbManager.GetStatusNameById(req.StatusId) ?? "Неизвестно").Trim();

                        var reportItem = new ReportRequestItem
                        {
                            RequestId = req.Id,
                            ClientName = client?.FullName ?? "Неизвестно",
                            CarInfo = carInfo,
                            StartDate = req.StartDate,
                            EndDate = req.EndDate,
                            Status = statusName,
                            TotalCost = req.TotalCost
                        };

                        var workItems = DbManager.GetWorkItemsByRequestId(req.Id);
                        if (workItems != null)
                        {
                            foreach (var wi in workItems)
                            {
                                var service = wi.ServiceId.HasValue ? DbManager.GetServiceById(wi.ServiceId.Value) : null;
                                var employee = wi.EmployeeId.HasValue ? DbManager.GetEmployeeById(wi.EmployeeId.Value) : null;

                                reportItem.WorkItems.Add(new ReportWorkItem
                                {
                                    ServiceName = service?.Name ?? "Неизвестно",
                                    EmployeeName = employee?.FullName ?? "Не назначен",
                                    Cost = wi.Cost
                                });
                            }
                        }

                        _allRequests.Add(reportItem);
                    }
                }

                report.Requests = _allRequests;

                report.Summary = new ReportSummary
                {
                    TotalRequests = report.Requests.Count,
                    CompletedRequests = report.Requests.Count(r => r.Status?.Trim() == "Завершённые"),
                    InProgressRequests = report.Requests.Count(r => r.Status?.Trim() == "В работе"),
                    CancelledRequests = report.Requests.Count(r => r.Status?.Trim() == "Отменённые"),
                    TotalRevenue = report.Requests.Where(r => r.Status?.Trim() == "Завершённые").Sum(r => r.TotalCost),
                    AverageRequestValue = report.Requests.Count > 0
                        ? report.Requests.Where(r => r.Status?.Trim() == "Завершённые").Sum(r => r.TotalCost) / report.Requests.Count : 0,
                    UniqueClients = report.Requests.Select(r => r.ClientName).Distinct().Count(),
                    UniqueCars = report.Requests.Select(r => r.CarInfo).Distinct().Count(),
                    TotalWorkItems = report.Requests.Sum(r => r.WorkItems.Count),
                    ActiveEmployees = report.Requests.SelectMany(r => r.WorkItems)
                        .Where(w => !string.IsNullOrEmpty(w.EmployeeName) && w.EmployeeName != "Не назначен")
                        .Select(w => w.EmployeeName).Distinct().Count()
                };

                CurrentDetailedReport = report;
                FilterDetailedReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки детального отчёта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterDetailedReport()
        {
            if (_allRequests == null) return;

            FilteredRequests.Clear();

            var filtered = _allRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(r =>
                    r.RequestId.ToString().Contains(SearchText) ||
                    r.ClientName.ToLower().Contains(SearchText.ToLower()) ||
                    r.CarInfo.ToLower().Contains(SearchText.ToLower()));
            }

            if (StatusFilter != "Все")
            {
                filtered = filtered.Where(r => r.Status?.Trim() == StatusFilter);
            }

            foreach (var item in filtered)
            {
                FilteredRequests.Add(item);
            }
        }

        private void AddMetric(ExcelWorksheet ws, ref int row, string label, object value, string format)
        {
            ws.Cells[$"A{row}"].Value = label;
            ws.Cells[$"A{row}"].Style.Font.Bold = true;
            ws.Cells[$"B{row}"].Value = value;
            if (!string.IsNullOrEmpty(format))
                ws.Cells[$"B{row}"].Style.Numberformat.Format = format;
            row++;
        }

        // ====================== ЭКСПОРТ В EXCEL ======================

        private void ExportRevenueReportToExcel()
        {
            try
            {
                if (CurrentRevenueReport == null) return;

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчёт_по_выручке_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Отчёт по выручке");

                    ws.Cells["A1"].Value = "ОТЧЁТ ПО ВЫРУЧКЕ АВТОСЕРВИСА";
                    ws.Cells["A1:F1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 24;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A2"].Value = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
                    ws.Cells["A2:F2"].Merge = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 4;
                    ws.Cells[$"A{row}"].Value = "КЛЮЧЕВЫЕ ПОКАЗАТЕЛИ";
                    ws.Cells[$"A{row}:F{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    AddMetric(ws, ref row, "Общая выручка", CurrentRevenueReport.TotalRevenue, "#,##0.00 ₽");
                    AddMetric(ws, ref row, "Всего заявок", CurrentRevenueReport.TotalRequests, "0");
                    AddMetric(ws, ref row, "Средний чек", CurrentRevenueReport.AverageRequestValue, "#,##0.00 ₽");
                    AddMetric(ws, ref row, "Выручка в день", CurrentRevenueReport.AverageRevenuePerDay, "#,##0.00 ₽");

                    row += 2;

                    if (CurrentRevenueReport.TopServices.Any())
                    {
                        ws.Cells[$"A{row}"].Value = "ТОП-5 УСЛУГ ПО ВЫРУЧКЕ";
                        ws.Cells[$"A{row}:D{row}"].Merge = true;
                        ws.Cells[$"A{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}"].Style.Font.Size = 14;
                        row += 1;

                        ws.Cells[$"A{row}"].Value = "№"; ws.Cells[$"B{row}"].Value = "Услуга";
                        ws.Cells[$"C{row}"].Value = "Кол-во"; ws.Cells[$"D{row}"].Value = "Выручка";
                        ws.Cells[$"A{row}:D{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}:D{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[$"A{row}:D{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241));
                        row += 1;

                        foreach (var service in CurrentRevenueReport.TopServices)
                        {
                            ws.Cells[$"A{row}"].Value = service.Index;
                            ws.Cells[$"B{row}"].Value = service.ServiceName;
                            ws.Cells[$"C{row}"].Value = service.Count;
                            ws.Cells[$"D{row}"].Value = service.Revenue;
                            ws.Cells[$"D{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                            row++;
                        }
                        row += 2;
                    }

                    if (CurrentRevenueReport.TopEmployees.Any())
                    {
                        ws.Cells[$"A{row}"].Value = "ТОП-5 СОТРУДНИКОВ ПО ВЫРУЧКЕ";
                        ws.Cells[$"A{row}:D{row}"].Merge = true;
                        ws.Cells[$"A{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}"].Style.Font.Size = 14;
                        row += 1;

                        ws.Cells[$"A{row}"].Value = "№"; ws.Cells[$"B{row}"].Value = "Сотрудник";
                        ws.Cells[$"C{row}"].Value = "Работ"; ws.Cells[$"D{row}"].Value = "Выручка";
                        ws.Cells[$"A{row}:D{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}:D{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[$"A{row}:D{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241));
                        row += 1;

                        foreach (var emp in CurrentRevenueReport.TopEmployees)
                        {
                            ws.Cells[$"A{row}"].Value = emp.Index;
                            ws.Cells[$"B{row}"].Value = emp.EmployeeName;
                            ws.Cells[$"C{row}"].Value = emp.CompletedJobs;
                            ws.Cells[$"D{row}"].Value = emp.TotalRevenue;
                            ws.Cells[$"D{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                            row++;
                        }
                    }

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("✅ Отчёт по выручке успешно сохранён в Excel!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDetailedReportToExcel()
        {
            try
            {
                if (CurrentDetailedReport == null || !CurrentDetailedReport.Requests.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Детальный_отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Детальный отчёт");

                    ws.Cells["A1"].Value = "ДЕТАЛЬНЫЙ ОТЧЁТ АВТОСЕРВИСА";
                    ws.Cells["A1:H1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 20;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A2"].Value = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
                    ws.Cells["A2:H2"].Merge = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A3"].Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cells["A3:H3"].Merge = true;
                    ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 5;

                    // Сводка
                    ws.Cells[$"A{row}"].Value = "СВОДКА";
                    ws.Cells[$"A{row}:H{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    AddMetric(ws, ref row, "Всего заявок:", CurrentDetailedReport.Summary.TotalRequests, "0");
                    AddMetric(ws, ref row, "Завершённые:", CurrentDetailedReport.Summary.CompletedRequests, "0");
                    AddMetric(ws, ref row, "В работе:", CurrentDetailedReport.Summary.InProgressRequests, "0");
                    AddMetric(ws, ref row, "Отменённые:", CurrentDetailedReport.Summary.CancelledRequests, "0");
                    AddMetric(ws, ref row, "Общая выручка:", CurrentDetailedReport.Summary.TotalRevenue, "#,##0.00 ₽");
                    AddMetric(ws, ref row, "Средний чек:", CurrentDetailedReport.Summary.AverageRequestValue, "#,##0.00 ₽");
                    AddMetric(ws, ref row, "Уникальных клиентов:", CurrentDetailedReport.Summary.UniqueClients, "0");
                    AddMetric(ws, ref row, "Уникальных авто:", CurrentDetailedReport.Summary.UniqueCars, "0");
                    AddMetric(ws, ref row, "Всего работ:", CurrentDetailedReport.Summary.TotalWorkItems, "0");
                    AddMetric(ws, ref row, "Активных сотрудников:", CurrentDetailedReport.Summary.ActiveEmployees, "0");

                    row += 2;

                    // Таблица заявок
                    ws.Cells[$"A{row}"].Value = "СПИСОК ЗАЯВОК";
                    ws.Cells[$"A{row}:H{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 1;

                    ws.Cells[$"A{row}"].Value = "№";
                    ws.Cells[$"B{row}"].Value = "Клиент";
                    ws.Cells[$"C{row}"].Value = "Автомобиль";
                    ws.Cells[$"D{row}"].Value = "Дата";
                    ws.Cells[$"E{row}"].Value = "Статус";
                    ws.Cells[$"F{row}"].Value = "Стоимость";
                    ws.Cells[$"G{row}"].Value = "Кол-во работ";
                    ws.Cells[$"H{row}"].Value = "Работы";

                    ws.Cells[$"A{row}:H{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:H{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}:H{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241));
                    row += 1;

                    foreach (var req in CurrentDetailedReport.Requests)
                    {
                        ws.Cells[$"A{row}"].Value = req.RequestId;
                        ws.Cells[$"B{row}"].Value = req.ClientName;
                        ws.Cells[$"C{row}"].Value = req.CarInfo;
                        ws.Cells[$"D{row}"].Value = req.StartDate.ToString("dd.MM.yyyy");
                        ws.Cells[$"E{row}"].Value = req.Status;
                        ws.Cells[$"F{row}"].Value = req.TotalCost;
                        ws.Cells[$"F{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                        ws.Cells[$"G{row}"].Value = req.WorkItems.Count;

                        var works = string.Join(Environment.NewLine, req.WorkItems.Select(w =>
                            $"{w.ServiceName} ({w.EmployeeName}): {w.Cost:N2} ₽"));

                        ws.Cells[$"H{row}"].Value = works;
                        ws.Cells[$"H{row}"].Style.WrapText = true;
                        row++;
                    }

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("✅ Детальный отчёт успешно сохранён в Excel!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSingleRequestToExcel()
        {
            try
            {
                if (SelectedRequest == null) return;

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Заказ-наряд_{SelectedRequest.RequestId}_{DateTime.Now:yyyyMMdd}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Заказ-наряд");

                    ws.Cells["A1"].Value = "АВТОСЕРВИС JDM TERRITORY";
                    ws.Cells["A1:E1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 20;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A2"].Value = $"ЗАКАЗ-НАРЯД № {SelectedRequest.RequestId}";
                    ws.Cells["A2:E2"].Merge = true;
                    ws.Cells["A2"].Style.Font.Size = 16;
                    ws.Cells["A2"].Style.Font.Bold = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A3"].Value = $"Дата: {SelectedRequest.StartDate:dd.MM.yyyy}";
                    ws.Cells["A3:E3"].Merge = true;
                    ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 5;

                    ws.Cells[$"A{row}"].Value = "ИНФОРМАЦИЯ О КЛИЕНТЕ";
                    ws.Cells[$"A{row}:E{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    ws.Cells[$"A{row}"].Value = "Клиент:"; ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.ClientName;
                    ws.Cells[$"B{row}:E{row}"].Merge = true; row++;

                    ws.Cells[$"A{row}"].Value = "Автомобиль:"; ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.CarInfo;
                    ws.Cells[$"B{row}:E{row}"].Merge = true; row++;

                    ws.Cells[$"A{row}"].Value = "Статус:"; ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.Status;
                    ws.Cells[$"B{row}:E{row}"].Merge = true; row += 2;

                    ws.Cells[$"A{row}"].Value = "ВЫПОЛНЕННЫЕ РАБОТЫ";
                    ws.Cells[$"A{row}:E{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 204, 113));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 1;

                    ws.Cells[$"A{row}"].Value = "№";
                    ws.Cells[$"B{row}"].Value = "Услуга";
                    ws.Cells[$"C{row}"].Value = "Сотрудник";
                    ws.Cells[$"D{row}"].Value = "Стоимость";
                    ws.Cells[$"E{row}"].Value = "Статус";
                    ws.Cells[$"A{row}:E{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:E{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}:E{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241));
                    row += 1;

                    int workNumber = 1;
                    foreach (var work in SelectedRequest.WorkItems)
                    {
                        ws.Cells[$"A{row}"].Value = workNumber++;
                        ws.Cells[$"B{row}"].Value = work.ServiceName;
                        ws.Cells[$"C{row}"].Value = work.EmployeeName;
                        ws.Cells[$"D{row}"].Value = work.Cost;
                        ws.Cells[$"D{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                        ws.Cells[$"E{row}"].Value = "Выполнено";
                        row++;
                    }

                    row++;
                    ws.Cells[$"D{row}"].Value = "ИТОГО:";
                    ws.Cells[$"D{row}"].Style.Font.Bold = true;
                    ws.Cells[$"E{row}"].Value = SelectedRequest.TotalCost;
                    ws.Cells[$"E{row}"].Style.Font.Bold = true;
                    ws.Cells[$"E{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                    ws.Cells[$"E{row}"].Style.Font.Color.SetColor(Color.FromArgb(231, 76, 60));

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("✅ Заказ-наряд успешно сохранён в Excel!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ====================== ЭКСПОРТ В WORD ======================

        private void ExportRevenueReportToWord()
        {
            try
            {
                if (CurrentRevenueReport == null) return;

                var dialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Отчёт_по_выручке_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
                };

                if (dialog.ShowDialog() != true) return;

                var wordApp = new Word.Application { Visible = false };
                var doc = wordApp.Documents.Add();

                var title = doc.Paragraphs.Add();
                title.Range.Text = "ОТЧЁТ ПО ВЫРУЧКЕ АВТОСЕРВИСА\n\n";
                title.Range.Font.Size = 24;
                title.Range.Font.Bold = 1;
                title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.InsertParagraphAfter();

                var period = doc.Paragraphs.Add();
                period.Range.Text = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}\n\n";
                period.Range.Font.Size = 14;
                period.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                period.Range.InsertParagraphAfter();

                var metrics = doc.Paragraphs.Add();
                metrics.Range.Text = "КЛЮЧЕВЫЕ ПОКАЗАТЕЛИ\n";
                metrics.Range.Font.Size = 16;
                metrics.Range.Font.Bold = 1;
                metrics.Range.InsertParagraphAfter();

                metrics.Range.Text += $"Общая выручка: {CurrentRevenueReport.TotalRevenue:N2} ₽\n";
                metrics.Range.Text += $"Всего заявок: {CurrentRevenueReport.TotalRequests}\n";
                metrics.Range.Text += $"Средний чек: {CurrentRevenueReport.AverageRequestValue:N2} ₽\n";
                metrics.Range.Text += $"Выручка в день: {CurrentRevenueReport.AverageRevenuePerDay:N2} ₽\n\n";

                if (CurrentRevenueReport.TopServices.Any())
                {
                    metrics.Range.Text += "ТОП-5 УСЛУГ ПО ВЫРУЧКЕ\n";
                    foreach (var service in CurrentRevenueReport.TopServices)
                    {
                        metrics.Range.Text += $"{service.Index}. {service.ServiceName} - {service.Count} раз(а) - {service.Revenue:N2} ₽\n";
                    }
                    metrics.Range.Text += "\n";
                }

                if (CurrentRevenueReport.TopEmployees.Any())
                {
                    metrics.Range.Text += "ТОП-5 СОТРУДНИКОВ ПО ВЫРУЧКЕ\n";
                    foreach (var emp in CurrentRevenueReport.TopEmployees)
                    {
                        metrics.Range.Text += $"{emp.Index}. {emp.EmployeeName} - {emp.CompletedJobs} работ - {emp.TotalRevenue:N2} ₽\n";
                    }
                }

                doc.SaveAs2(dialog.FileName);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show("✅ Отчёт по выручке успешно сохранён в Word!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDetailedReportToWord()
        {
            try
            {
                if (CurrentDetailedReport == null || !CurrentDetailedReport.Requests.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Детальный_отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
                };

                if (dialog.ShowDialog() != true) return;

                var wordApp = new Word.Application { Visible = false };
                var doc = wordApp.Documents.Add();

                var title = doc.Paragraphs.Add();
                title.Range.Text = "ДЕТАЛЬНЫЙ ОТЧЁТ АВТОСЕРВИСА\n\n";
                title.Range.Font.Size = 24;
                title.Range.Font.Bold = 1;
                title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.InsertParagraphAfter();

                var period = doc.Paragraphs.Add();
                period.Range.Text = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}\n\n";
                period.Range.Font.Size = 14;
                period.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                period.Range.InsertParagraphAfter();

                var summary = doc.Paragraphs.Add();
                summary.Range.Text = "СВОДКА\n";
                summary.Range.Font.Size = 16;
                summary.Range.Font.Bold = 1;
                summary.Range.InsertParagraphAfter();

                summary.Range.Text += $"Всего заявок: {CurrentDetailedReport.Summary.TotalRequests}\n";
                summary.Range.Text += $"Завершённые: {CurrentDetailedReport.Summary.CompletedRequests}\n";
                summary.Range.Text += $"В работе: {CurrentDetailedReport.Summary.InProgressRequests}\n";
                summary.Range.Text += $"Отменённые: {CurrentDetailedReport.Summary.CancelledRequests}\n";
                summary.Range.Text += $"Общая выручка: {CurrentDetailedReport.Summary.TotalRevenue:N2} ₽\n";
                summary.Range.Text += $"Средний чек: {CurrentDetailedReport.Summary.AverageRequestValue:N2} ₽\n\n";

                summary.Range.Text += "СПИСОК ЗАЯВОК\n\n";

                foreach (var req in CurrentDetailedReport.Requests.Take(50))
                {
                    summary.Range.Text += $"№{req.RequestId} - {req.ClientName} - {req.CarInfo}\n";
                    summary.Range.Text += $"Дата: {req.StartDate:dd.MM.yyyy}, Статус: {req.Status}, Стоимость: {req.TotalCost:N2} ₽\n";
                    if (req.WorkItems.Any())
                    {
                        summary.Range.Text += "Работы:\n";
                        foreach (var work in req.WorkItems.Take(5))
                        {
                            summary.Range.Text += $"  • {work.ServiceName} (Сотрудник: {work.EmployeeName}, Стоимость: {work.Cost:N2} ₽)\n";
                        }
                        if (req.WorkItems.Count > 5)
                            summary.Range.Text += $"  ... и ещё {req.WorkItems.Count - 5} работ(ы)\n";
                    }
                    summary.Range.Text += "\n";
                }

                if (CurrentDetailedReport.Requests.Count > 50)
                    summary.Range.Text += $"\n... и ещё {CurrentDetailedReport.Requests.Count - 50} заявок\n";

                doc.SaveAs2(dialog.FileName);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show("✅ Детальный отчёт успешно сохранён в Word!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSingleRequestToWord()
        {
            try
            {
                if (SelectedRequest == null) return;

                var dialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Заказ-наряд_{SelectedRequest.RequestId}_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (dialog.ShowDialog() != true) return;

                var wordApp = new Word.Application { Visible = false };
                var doc = wordApp.Documents.Add();

                var title = doc.Paragraphs.Add();
                title.Range.Text = "АВТОСЕРВИС JDM TERRITORY\n";
                title.Range.Font.Size = 20;
                title.Range.Font.Bold = 1;
                title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.InsertParagraphAfter();

                var orderNumber = doc.Paragraphs.Add();
                orderNumber.Range.Text = $"ЗАКАЗ-НАРЯД № {SelectedRequest.RequestId}\n";
                orderNumber.Range.Font.Size = 18;
                orderNumber.Range.Font.Bold = 1;
                orderNumber.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                orderNumber.Range.InsertParagraphAfter();

                var date = doc.Paragraphs.Add();
                date.Range.Text = $"Дата: {SelectedRequest.StartDate:dd.MM.yyyy}\n\n";
                date.Range.Font.Size = 12;
                date.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                date.Range.InsertParagraphAfter();

                var clientTable = doc.Tables.Add(doc.Paragraphs.Last.Range, 4, 2);
                clientTable.Borders.Enable = 1;
                clientTable.Rows[1].Cells[1].Range.Text = "Клиент:";
                clientTable.Rows[1].Cells[1].Range.Font.Bold = 1;
                clientTable.Rows[1].Cells[2].Range.Text = SelectedRequest.ClientName;
                clientTable.Rows[2].Cells[1].Range.Text = "Автомобиль:";
                clientTable.Rows[2].Cells[1].Range.Font.Bold = 1;
                clientTable.Rows[2].Cells[2].Range.Text = SelectedRequest.CarInfo;
                clientTable.Rows[3].Cells[1].Range.Text = "Статус:";
                clientTable.Rows[3].Cells[1].Range.Font.Bold = 1;
                clientTable.Rows[3].Cells[2].Range.Text = SelectedRequest.Status;
                clientTable.Rows[4].Cells[1].Range.Text = "Статус оплаты:";
                clientTable.Rows[4].Cells[1].Range.Font.Bold = 1;
                clientTable.Rows[4].Cells[2].Range.Text = "Ожидает оплаты";

                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                var workTable = doc.Tables.Add(doc.Paragraphs.Last.Range, SelectedRequest.WorkItems.Count + 1, 4);
                workTable.Borders.Enable = 1;

                workTable.Rows[1].Cells[1].Range.Text = "№";
                workTable.Rows[1].Cells[2].Range.Text = "Услуга";
                workTable.Rows[1].Cells[3].Range.Text = "Сотрудник";
                workTable.Rows[1].Cells[4].Range.Text = "Стоимость";
                workTable.Rows[1].Range.Font.Bold = 1;

                int workNumber = 1;
                int workRow = 2;
                foreach (var work in SelectedRequest.WorkItems)
                {
                    workTable.Rows[workRow].Cells[1].Range.Text = workNumber.ToString();
                    workTable.Rows[workRow].Cells[2].Range.Text = work.ServiceName;
                    workTable.Rows[workRow].Cells[3].Range.Text = work.EmployeeName;
                    workTable.Rows[workRow].Cells[4].Range.Text = $"{work.Cost:N2} ₽";
                    workRow++;
                    workNumber++;
                }

                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                var total = doc.Paragraphs.Add();
                total.Range.Text = $"\nИТОГО: {SelectedRequest.TotalCost:N2} ₽";
                total.Range.Font.Size = 14;
                total.Range.Font.Bold = 1;
                total.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                var signatures = doc.Paragraphs.Add();
                signatures.Range.Text = $"\n\n\n\n\nМастер: ______________________\nКлиент: ______________________";
                signatures.Range.Font.Size = 11;

                doc.SaveAs2(dialog.FileName);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show("✅ Заказ-наряд успешно создан в Word!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}\n\nУбедитесь, что установлен Microsoft Word",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
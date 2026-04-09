// ReportsViewModel.cs — ПОЛНАЯ ВЕРСИЯ С ДИАГРАММАМИ (БЕЗ ДЕТАЛЬНОГО ОТЧЕТА WORD)
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
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
        public ICommand ExportSingleRequestToExcelCommand { get; private set; }
        public ICommand ExportSingleRequestToWordCommand { get; private set; }
        public ICommand ExportVehiclesReportCommand { get; private set; }
        public ICommand ExportEmployeesReportCommand { get; private set; }

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
            ExportSingleRequestToExcelCommand = new RelayCommand(_ => ExportSingleRequestToExcel(), _ => CanExportSingleRequest);
            ExportSingleRequestToWordCommand = new RelayCommand(_ => ExportSingleRequestToWord(), _ => CanExportSingleRequest);
            ExportVehiclesReportCommand = new RelayCommand(_ => ExportVehiclesReportToExcel());
            ExportEmployeesReportCommand = new RelayCommand(_ => ExportEmployeesReportToExcel());
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

                // Данные для диаграммы выручки по дням
                report.RevenueChartData = new List<ChartDataItem>();
                for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
                {
                    var dayRevenue = completedRequests.Where(r => r.StartDate.Date == date.Date).Sum(r => r.TotalCost);
                    report.RevenueChartData.Add(new ChartDataItem
                    {
                        Label = date.ToString("dd.MM"),
                        Value = dayRevenue,
                        Count = completedRequests.Count(r => r.StartDate.Date == date.Date)
                    });
                }

                // Данные для диаграммы по статусам
                report.StatusChartData = requests
                    .GroupBy(r => r.Status?.Name ?? "Неизвестно")
                    .Select(g => new ChartDataItem
                    {
                        Label = g.Key,
                        Count = g.Count(),
                        Value = g.Where(r => r.StatusId == 3).Sum(r => r.TotalCost)
                    })
                    .ToList();

                report.RequestsByStatus = requests
                    .GroupBy(r => r.Status?.Name ?? "Неизвестно")
                    .ToDictionary(g => g.Key, g => g.Count());

                var allWorkItems = new List<WorkItem>();
                foreach (var req in requests)
                {
                    var workItems = DbManager.GetWorkItemsByRequestId(req.Id);
                    if (workItems != null) allWorkItems.AddRange(workItems);
                }

                // Данные для диаграммы топ услуг
                report.ServicesChartData = allWorkItems
                    .Where(wi => wi.ServiceId.HasValue)
                    .GroupBy(wi => wi.ServiceId.Value)
                    .Select(g => new ChartDataItem
                    {
                        Label = GetServiceNameById(g.Key),
                        Value = g.Sum(wi => wi.Cost),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .ToList();

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
        private string GetEmployeeNameById(int employeeId)
        {
            var employee = DbManager.GetEmployeeById(employeeId);
            return employee != null ? $"{employee.FirstName} {employee.LastName}" : "Неизвестно";
        }
        private string GetClientNameById(int clientId)
        {
            var client = DbManager.GetClientById(clientId);
            return client != null ? $"{client.FirstName} {client.LastName}" : "Неизвестно";
        }
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
                                    EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Не назначен",
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

        // ====================== ЭКСПОРТ В EXCEL С ДИАГРАММАМИ ======================

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

                    // Заголовок
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

                    // Диаграмма выручки по дням
                    if (CurrentRevenueReport.RevenueChartData.Any())
                    {
                        ws.Cells[$"A{row}"].Value = "ДИНАМИКА ВЫРУЧКИ ПО ДНЯМ";
                        ws.Cells[$"A{row}:F{row}"].Merge = true;
                        ws.Cells[$"A{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}"].Style.Font.Size = 14;
                        row += 1;

                        int dataStartRow = row;
                        ws.Cells[$"A{dataStartRow}"].Value = "Дата";
                        ws.Cells[$"B{dataStartRow}"].Value = "Выручка";
                        ws.Cells[$"C{dataStartRow}"].Value = "Кол-во заявок";
                        ws.Cells[$"A{dataStartRow}:C{dataStartRow}"].Style.Font.Bold = true;
                        row += 1;

                        foreach (var item in CurrentRevenueReport.RevenueChartData)
                        {
                            ws.Cells[$"A{row}"].Value = item.Label;
                            ws.Cells[$"B{row}"].Value = item.Value;
                            ws.Cells[$"C{row}"].Value = item.Count;
                            row++;
                        }

                        var chart = ws.Drawings.AddChart("RevenueChart", eChartType.ColumnClustered);
                        chart.Title.Text = "Выручка по дням";
                        chart.SetPosition(dataStartRow - 2, 0, 4, 0);
                        chart.SetSize(500, 300);
                        chart.Series.Add(ExcelRange.GetAddress(dataStartRow + 1, 2, row - 1, 2),
                                        ExcelRange.GetAddress(dataStartRow + 1, 1, row - 1, 1));

                        row += 15;
                    }

                    row += 2;

                    // Диаграмма по статусам
                    if (CurrentRevenueReport.StatusChartData.Any())
                    {
                        ws.Cells[$"A{row}"].Value = "СТАТУСЫ ЗАЯВОК";
                        ws.Cells[$"A{row}:F{row}"].Merge = true;
                        ws.Cells[$"A{row}"].Style.Font.Bold = true;
                        ws.Cells[$"A{row}"].Style.Font.Size = 14;
                        row += 1;

                        int dataStartRow = row;
                        ws.Cells[$"A{dataStartRow}"].Value = "Статус";
                        ws.Cells[$"B{dataStartRow}"].Value = "Количество";
                        ws.Cells[$"C{dataStartRow}"].Value = "Выручка";
                        ws.Cells[$"A{dataStartRow}:C{dataStartRow}"].Style.Font.Bold = true;
                        row += 1;

                        foreach (var item in CurrentRevenueReport.StatusChartData)
                        {
                            ws.Cells[$"A{row}"].Value = item.Label;
                            ws.Cells[$"B{row}"].Value = item.Count;
                            ws.Cells[$"C{row}"].Value = item.Value;
                            row++;
                        }

                        var pieChart = ws.Drawings.AddChart("StatusChart", eChartType.Pie);
                        pieChart.Title.Text = "Распределение заявок по статусам";
                        pieChart.SetPosition(dataStartRow - 2, 0, 4, 0);
                        pieChart.SetSize(400, 300);
                        pieChart.Series.Add(ExcelRange.GetAddress(dataStartRow + 1, 2, row - 1, 2),
                                            ExcelRange.GetAddress(dataStartRow + 1, 1, row - 1, 1));

                        row += 15;
                    }

                    row += 2;

                    // Топ услуги
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

                    // Топ сотрудники
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

        // ====================== НОВЫЕ ОТЧЁТЫ ======================

        private void ExportVehiclesReportToExcel()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчёт_по_автомобилям_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                var cars = DbManager.GetAllCars();

                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Автомобили");

                    ws.Cells["A1"].Value = "ОТЧЁТ ПО АВТОМОБИЛЯМ";
                    ws.Cells["A1:E1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 20;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A2"].Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cells["A2:E2"].Merge = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 4;
                    ws.Cells[$"A{row}"].Value = "СТАТИСТИКА ПО МАРКАМ";
                    ws.Cells[$"A{row}:E{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    var carStats = cars.GroupBy(c => c.Brand)
                        .Select(g => new { Brand = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    ws.Cells[$"A{row}"].Value = "Марка";
                    ws.Cells[$"B{row}"].Value = "Количество";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
                    row += 1;

                    foreach (var stat in carStats)
                    {
                        ws.Cells[$"A{row}"].Value = stat.Brand;
                        ws.Cells[$"B{row}"].Value = stat.Count;
                        row++;
                    }

                    row += 2;

                    ws.Cells[$"A{row}"].Value = "СПИСОК АВТОМОБИЛЕЙ";
                    ws.Cells[$"A{row}:E{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 1;

                    ws.Cells[$"A{row}"].Value = "№";
                    ws.Cells[$"B{row}"].Value = "Марка";
                    ws.Cells[$"C{row}"].Value = "Модель";
                    ws.Cells[$"D{row}"].Value = "Госномер";
                    ws.Cells[$"E{row}"].Value = "Владелец";
                    ws.Cells[$"A{row}:E{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:E{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}:E{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241));
                    row += 1;

                    int num = 1;
                    foreach (var car in cars)
                    {
                        var client = DbManager.GetClientById(car.OwnerId);
                        ws.Cells[$"A{row}"].Value = num++;
                        ws.Cells[$"B{row}"].Value = car.Brand;
                        ws.Cells[$"C{row}"].Value = car.Model;
                        ws.Cells[$"D{row}"].Value = car.RegistrationNumber;
                        ws.Cells[$"E{row}"].Value = client != null ? $"{client.FirstName} {client.LastName}" : "Не указан";
                        row++;
                    }

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("✅ Отчёт по автомобилям успешно сохранён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportEmployeesReportToExcel()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчёт_по_сотрудникам_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                var workItems = DbManager.GetAllWorkItems();

                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Сотрудники");

                    ws.Cells["A1"].Value = "ОТЧЁТ ПО СОТРУДНИКАМ";
                    ws.Cells["A1:D1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 20;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells["A2"].Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cells["A2:D2"].Merge = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 4;
                    ws.Cells[$"A{row}"].Value = "РЕЙТИНГ СОТРУДНИКОВ";
                    ws.Cells[$"A{row}:D{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    var employeeStats = workItems
                        .Where(wi => wi.EmployeeId.HasValue)
                        .GroupBy(wi => wi.EmployeeId.Value)
                        .Select(g => new
                        {
                            EmployeeId = g.Key,
                            EmployeeName = GetEmployeeNameById(g.Key),
                            CompletedJobs = g.Count(),
                            TotalRevenue = g.Sum(wi => wi.Cost)
                        })
                        .OrderByDescending(x => x.TotalRevenue)
                        .ToList();

                    ws.Cells[$"A{row}"].Value = "№";
                    ws.Cells[$"B{row}"].Value = "Сотрудник";
                    ws.Cells[$"C{row}"].Value = "Выполнено работ";
                    ws.Cells[$"D{row}"].Value = "Выручка";
                    ws.Cells[$"A{row}:D{row}"].Style.Font.Bold = true;
                    row += 1;

                    int num = 1;
                    foreach (var stat in employeeStats)
                    {
                        ws.Cells[$"A{row}"].Value = num++;
                        ws.Cells[$"B{row}"].Value = stat.EmployeeName;
                        ws.Cells[$"C{row}"].Value = stat.CompletedJobs;
                        ws.Cells[$"D{row}"].Value = stat.TotalRevenue;
                        ws.Cells[$"D{row}"].Style.Numberformat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("✅ Отчёт по сотрудникам успешно сохранён!", "Успех",
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

                // Устанавливаем ориентацию альбомная для лучшего отображения
                doc.PageSetup.Orientation = Word.WdOrientation.wdOrientLandscape;
                doc.PageSetup.LeftMargin = 50f;
                doc.PageSetup.RightMargin = 50f;
                doc.PageSetup.TopMargin = 50f;
                doc.PageSetup.BottomMargin = 50f;

                // ====================== ЗАГОЛОВОК ======================
                var title = doc.Paragraphs.Add();
                title.Range.Text = "ОТЧЁТ ПО ВЫРУЧКЕ АВТОСЕРВИСА\n\n";
                title.Range.Font.Size = 28;
                title.Range.Font.Bold = 1;
                title.Range.Font.Name = "Times New Roman";
                title.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.InsertParagraphAfter();

                // ====================== ПЕРИОД ======================
                var period = doc.Paragraphs.Add();
                period.Range.Text = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}\n";
                period.Range.Font.Size = 14;
                period.Range.Font.Name = "Times New Roman";
                period.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                period.Range.InsertParagraphAfter();

                // ====================== ДАТА ФОРМИРОВАНИЯ ======================
                var dateGen = doc.Paragraphs.Add();
                dateGen.Range.Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n";
                dateGen.Range.Font.Size = 11;
                dateGen.Range.Font.Name = "Times New Roman";
                dateGen.Range.Font.Italic = 1;
                dateGen.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                dateGen.Range.InsertParagraphAfter();

                // ====================== ТАБЛИЦА КЛЮЧЕВЫХ ПОКАЗАТЕЛЕЙ ======================
                var metricsTitle = doc.Paragraphs.Add();
                metricsTitle.Range.Text = "КЛЮЧЕВЫЕ ПОКАЗАТЕЛИ";
                metricsTitle.Range.Font.Size = 18;
                metricsTitle.Range.Font.Bold = 1;
                metricsTitle.Range.Font.Name = "Times New Roman";
                metricsTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                metricsTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTitle.Range.InsertParagraphAfter();

                // Создаем красивую таблицу метрик
                var metricsTable = doc.Tables.Add(doc.Paragraphs.Last.Range, 2, 4);
                metricsTable.Borders.Enable = 1;
                metricsTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                metricsTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                metricsTable.AllowPageBreaks = false;

                // Заливка заголовка таблицы
                metricsTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x2C3E50; // Темно-синий
                metricsTable.Rows[1].Range.Font.Bold = 1;
                metricsTable.Rows[1].Range.Font.Size = 14;
                metricsTable.Rows[1].Range.Font.Name = "Times New Roman";
                metricsTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                metricsTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                metricsTable.Cell(1, 1).Range.Text = "Общая выручка";
                metricsTable.Cell(1, 2).Range.Text = "Всего заявок";
                metricsTable.Cell(1, 3).Range.Text = "Средний чек";
                metricsTable.Cell(1, 4).Range.Text = "Выручка в день";

                // Данные
                metricsTable.Rows[2].Range.Font.Size = 16;
                metricsTable.Rows[2].Range.Font.Bold = 1;
                metricsTable.Rows[2].Range.Font.Name = "Times New Roman";
                metricsTable.Rows[2].Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                metricsTable.Rows[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTable.Rows[2].Shading.BackgroundPatternColor = (Word.WdColor)0xE8F4FD; // Светло-голубой

                metricsTable.Cell(2, 1).Range.Text = $"{CurrentRevenueReport.TotalRevenue:N2} ₽";
                metricsTable.Cell(2, 2).Range.Text = CurrentRevenueReport.TotalRequests.ToString();
                metricsTable.Cell(2, 3).Range.Text = $"{CurrentRevenueReport.AverageRequestValue:N2} ₽";
                metricsTable.Cell(2, 4).Range.Text = $"{CurrentRevenueReport.AverageRevenuePerDay:N2} ₽";

                // Автоподбор ширины
                metricsTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                // ====================== ДИАГРАММА ВЫРУЧКИ ПО ДНЯМ (в виде таблицы) ======================
                if (CurrentRevenueReport.RevenueChartData.Any())
                {
                    var revenueTitle = doc.Paragraphs.Add();
                    revenueTitle.Range.Text = "ДИНАМИКА ВЫРУЧКИ ПО ДНЯМ";
                    revenueTitle.Range.Font.Size = 16;
                    revenueTitle.Range.Font.Bold = 1;
                    revenueTitle.Range.Font.Name = "Times New Roman";
                    revenueTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                    revenueTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    revenueTitle.Range.InsertParagraphAfter();

                    var revenueTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.RevenueChartData.Count + 1, 3);
                    revenueTable.Borders.Enable = 1;
                    revenueTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    revenueTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    // Заголовок
                    revenueTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x3498DB; // Синий
                    revenueTable.Rows[1].Range.Font.Bold = 1;
                    revenueTable.Rows[1].Range.Font.Size = 13;
                    revenueTable.Rows[1].Range.Font.Name = "Times New Roman";
                    revenueTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    revenueTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    revenueTable.Cell(1, 1).Range.Text = "Дата";
                    revenueTable.Cell(1, 2).Range.Text = "Выручка";
                    revenueTable.Cell(1, 3).Range.Text = "Кол-во заявок";

                    int currentRow = 2;
                    foreach (var item in CurrentRevenueReport.RevenueChartData)
                    {
                        revenueTable.Rows[currentRow].Range.Font.Size = 11;
                        revenueTable.Rows[currentRow].Range.Font.Name = "Times New Roman";
                        revenueTable.Rows[currentRow].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                        if (currentRow % 2 == 0)
                            revenueTable.Rows[currentRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        revenueTable.Cell(currentRow, 1).Range.Text = item.Label;
                        revenueTable.Cell(currentRow, 2).Range.Text = $"{item.Value:N2} ₽";
                        revenueTable.Cell(currentRow, 3).Range.Text = item.Count.ToString();
                        currentRow++;
                    }

                    revenueTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                }

                // ====================== ТАБЛИЦА СТАТУСОВ ======================
                if (CurrentRevenueReport.StatusChartData.Any())
                {
                    var statusTitle = doc.Paragraphs.Add();
                    statusTitle.Range.Text = "РАСПРЕДЕЛЕНИЕ ЗАЯВОК ПО СТАТУСАМ";
                    statusTitle.Range.Font.Size = 16;
                    statusTitle.Range.Font.Bold = 1;
                    statusTitle.Range.Font.Name = "Times New Roman";
                    statusTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                    statusTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    statusTitle.Range.InsertParagraphAfter();

                    var statusTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.StatusChartData.Count + 1, 3);
                    statusTable.Borders.Enable = 1;
                    statusTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    statusTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    statusTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x9B59B6; // Фиолетовый
                    statusTable.Rows[1].Range.Font.Bold = 1;
                    statusTable.Rows[1].Range.Font.Size = 13;
                    statusTable.Rows[1].Range.Font.Name = "Times New Roman";
                    statusTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    statusTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    statusTable.Cell(1, 1).Range.Text = "Статус";
                    statusTable.Cell(1, 2).Range.Text = "Количество";
                    statusTable.Cell(1, 3).Range.Text = "Выручка";

                    int currentRow = 2;
                    foreach (var item in CurrentRevenueReport.StatusChartData)
                    {
                        statusTable.Rows[currentRow].Range.Font.Size = 11;
                        statusTable.Rows[currentRow].Range.Font.Name = "Times New Roman";
                        statusTable.Rows[currentRow].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                        if (currentRow % 2 == 0)
                            statusTable.Rows[currentRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        statusTable.Cell(currentRow, 1).Range.Text = item.Label;
                        statusTable.Cell(currentRow, 2).Range.Text = item.Count.ToString();
                        statusTable.Cell(currentRow, 3).Range.Text = $"{item.Value:N2} ₽";
                        currentRow++;
                    }

                    statusTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                }

                // ====================== ТОП-5 УСЛУГ ======================
                if (CurrentRevenueReport.TopServices.Any())
                {
                    var servicesTitle = doc.Paragraphs.Add();
                    servicesTitle.Range.Text = "ТОП-5 УСЛУГ ПО ВЫРУЧКЕ";
                    servicesTitle.Range.Font.Size = 16;
                    servicesTitle.Range.Font.Bold = 1;
                    servicesTitle.Range.Font.Name = "Times New Roman";
                    servicesTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                    servicesTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    servicesTitle.Range.InsertParagraphAfter();

                    var servicesTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.TopServices.Count + 1, 4);
                    servicesTable.Borders.Enable = 1;
                    servicesTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    servicesTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    servicesTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x27AE60; // Зеленый
                    servicesTable.Rows[1].Range.Font.Bold = 1;
                    servicesTable.Rows[1].Range.Font.Size = 13;
                    servicesTable.Rows[1].Range.Font.Name = "Times New Roman";
                    servicesTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    servicesTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    servicesTable.Cell(1, 1).Range.Text = "№";
                    servicesTable.Cell(1, 2).Range.Text = "Услуга";
                    servicesTable.Cell(1, 3).Range.Text = "Кол-во";
                    servicesTable.Cell(1, 4).Range.Text = "Выручка";

                    int currentRow = 2;
                    foreach (var service in CurrentRevenueReport.TopServices)
                    {
                        servicesTable.Rows[currentRow].Range.Font.Size = 11;
                        servicesTable.Rows[currentRow].Range.Font.Name = "Times New Roman";

                        if (currentRow % 2 == 0)
                            servicesTable.Rows[currentRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        servicesTable.Cell(currentRow, 1).Range.Text = service.Index.ToString();
                        servicesTable.Cell(currentRow, 2).Range.Text = service.ServiceName;
                        servicesTable.Cell(currentRow, 3).Range.Text = service.Count.ToString();
                        servicesTable.Cell(currentRow, 4).Range.Text = $"{service.Revenue:N2} ₽";
                        servicesTable.Cell(currentRow, 4).Range.Font.Bold = 1;
                        servicesTable.Cell(currentRow, 4).Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                        currentRow++;
                    }

                    servicesTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                }

                // ====================== ТОП-5 СОТРУДНИКОВ ======================
                if (CurrentRevenueReport.TopEmployees.Any())
                {
                    var employeesTitle = doc.Paragraphs.Add();
                    employeesTitle.Range.Text = "ТОП-5 СОТРУДНИКОВ ПО ВЫРУЧКЕ";
                    employeesTitle.Range.Font.Size = 16;
                    employeesTitle.Range.Font.Bold = 1;
                    employeesTitle.Range.Font.Name = "Times New Roman";
                    employeesTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                    employeesTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    employeesTitle.Range.InsertParagraphAfter();

                    var employeesTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.TopEmployees.Count + 1, 4);
                    employeesTable.Borders.Enable = 1;
                    employeesTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    employeesTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    employeesTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0xE67E22; // Оранжевый
                    employeesTable.Rows[1].Range.Font.Bold = 1;
                    employeesTable.Rows[1].Range.Font.Size = 13;
                    employeesTable.Rows[1].Range.Font.Name = "Times New Roman";
                    employeesTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    employeesTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    employeesTable.Cell(1, 1).Range.Text = "№";
                    employeesTable.Cell(1, 2).Range.Text = "Сотрудник";
                    employeesTable.Cell(1, 3).Range.Text = "Выполнено работ";
                    employeesTable.Cell(1, 4).Range.Text = "Выручка";

                    int currentRow = 2;
                    foreach (var emp in CurrentRevenueReport.TopEmployees)
                    {
                        employeesTable.Rows[currentRow].Range.Font.Size = 11;
                        employeesTable.Rows[currentRow].Range.Font.Name = "Times New Roman";

                        if (currentRow % 2 == 0)
                            employeesTable.Rows[currentRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        employeesTable.Cell(currentRow, 1).Range.Text = emp.Index.ToString();
                        employeesTable.Cell(currentRow, 2).Range.Text = emp.EmployeeName;
                        employeesTable.Cell(currentRow, 3).Range.Text = emp.CompletedJobs.ToString();
                        employeesTable.Cell(currentRow, 4).Range.Text = $"{emp.TotalRevenue:N2} ₽";
                        employeesTable.Cell(currentRow, 4).Range.Font.Bold = 1;
                        employeesTable.Cell(currentRow, 4).Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                        currentRow++;
                    }

                    employeesTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                }

                // Сохраняем документ
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

                // Настройка страницы
                doc.PageSetup.Orientation = Word.WdOrientation.wdOrientPortrait;
                doc.PageSetup.LeftMargin = 70f;
                doc.PageSetup.RightMargin = 70f;
                doc.PageSetup.TopMargin = 50f;
                doc.PageSetup.BottomMargin = 50f;

                // ====================== ЗАГОЛОВОК ======================
                var title = doc.Paragraphs.Add();
                title.Range.Text = "АВТОСЕРВИС JDM TERRITORY\n";
                title.Range.Font.Size = 24;
                title.Range.Font.Bold = 1;
                title.Range.Font.Name = "Times New Roman";
                title.Range.Font.Color = Word.WdColor.wdColorDarkRed;
                title.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.InsertParagraphAfter();

                var orderNumber = doc.Paragraphs.Add();
                orderNumber.Range.Text = $"ЗАКАЗ-НАРЯД № {SelectedRequest.RequestId}";
                orderNumber.Range.Font.Size = 20;
                orderNumber.Range.Font.Bold = 1;
                orderNumber.Range.Font.Name = "Times New Roman";
                orderNumber.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                orderNumber.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                orderNumber.Range.InsertParagraphAfter();

                var date = doc.Paragraphs.Add();
                date.Range.Text = $"Дата создания: {SelectedRequest.StartDate:dd.MM.yyyy}";
                date.Range.Font.Size = 12;
                date.Range.Font.Name = "Times New Roman";
                date.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                date.Range.InsertParagraphAfter();
                date.Range.InsertParagraphAfter();

                // ====================== ИНФОРМАЦИЯ О КЛИЕНТЕ (КРАСИВАЯ ТАБЛИЦА) ======================
                var clientTitle = doc.Paragraphs.Add();
                clientTitle.Range.Text = "ИНФОРМАЦИЯ О КЛИЕНТЕ";
                clientTitle.Range.Font.Size = 16;
                clientTitle.Range.Font.Bold = 1;
                clientTitle.Range.Font.Name = "Times New Roman";
                clientTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                clientTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                clientTitle.Range.InsertParagraphAfter();

                var clientTable = doc.Tables.Add(doc.Paragraphs.Last.Range, 3, 2);
                clientTable.Borders.Enable = 1;
                clientTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                clientTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                // Стиль для заголовков таблицы
                clientTable.Columns[1].Width = 120;
                clientTable.Columns[2].Width = 350;

                for (int i = 1; i <= 3; i++)
                {
                    clientTable.Rows[i].Cells[1].Shading.BackgroundPatternColor = (Word.WdColor)0xEBF5FB;
                    clientTable.Rows[i].Cells[1].Range.Font.Bold = 1;
                    clientTable.Rows[i].Cells[1].Range.Font.Size = 12;
                    clientTable.Rows[i].Cells[1].Range.Font.Name = "Times New Roman";
                    clientTable.Rows[i].Cells[2].Range.Font.Size = 12;
                    clientTable.Rows[i].Cells[2].Range.Font.Name = "Times New Roman";
                }

                clientTable.Cell(1, 1).Range.Text = "Клиент:";
                clientTable.Cell(1, 2).Range.Text = SelectedRequest.ClientName;
                clientTable.Cell(2, 1).Range.Text = "Автомобиль:";
                clientTable.Cell(2, 2).Range.Text = SelectedRequest.CarInfo;
                clientTable.Cell(3, 1).Range.Text = "Статус заказа:";
                clientTable.Cell(3, 2).Range.Text = SelectedRequest.Status;

                // Цвет статуса
                if (SelectedRequest.Status == "Завершённые")
                    clientTable.Cell(3, 2).Shading.BackgroundPatternColor = (Word.WdColor)0xD5F5E3;
                else if (SelectedRequest.Status == "В работе")
                    clientTable.Cell(3, 2).Shading.BackgroundPatternColor = (Word.WdColor)0xEBF5FB;
                else if (SelectedRequest.Status == "Новые")
                    clientTable.Cell(3, 2).Shading.BackgroundPatternColor = (Word.WdColor)0xFEF9E7;
                else if (SelectedRequest.Status == "Отменённые")
                    clientTable.Cell(3, 2).Shading.BackgroundPatternColor = (Word.WdColor)0xFDEDEC;

                clientTable.Cell(3, 2).Range.Font.Bold = 1;

                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                // ====================== ВЫПОЛНЕННЫЕ РАБОТЫ ======================
                var workTitle = doc.Paragraphs.Add();
                workTitle.Range.Text = "ВЫПОЛНЕННЫЕ РАБОТЫ";
                workTitle.Range.Font.Size = 16;
                workTitle.Range.Font.Bold = 1;
                workTitle.Range.Font.Name = "Times New Roman";
                workTitle.Range.Font.Color = Word.WdColor.wdColorDarkBlue;
                workTitle.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                workTitle.Range.InsertParagraphAfter();

                var workTable = doc.Tables.Add(doc.Paragraphs.Last.Range, SelectedRequest.WorkItems.Count + 1, 5);
                workTable.Borders.Enable = 1;
                workTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                workTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                // Заголовки
                workTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x2C3E50;
                workTable.Rows[1].Range.Font.Bold = 1;
                workTable.Rows[1].Range.Font.Size = 13;
                workTable.Rows[1].Range.Font.Name = "Times New Roman";
                workTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                workTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                workTable.Cell(1, 1).Range.Text = "№";
                workTable.Cell(1, 2).Range.Text = "Услуга";
                workTable.Cell(1, 3).Range.Text = "Сотрудник";
                workTable.Cell(1, 4).Range.Text = "Стоимость";
                workTable.Cell(1, 5).Range.Text = "Статус";

                // Настройка ширины столбцов
                workTable.Columns[1].Width = 40;
                workTable.Columns[2].Width = 200;
                workTable.Columns[3].Width = 150;
                workTable.Columns[4].Width = 100;
                workTable.Columns[5].Width = 80;

                int workNumber = 1;
                int workRow = 2;
                decimal totalCost = 0;

                foreach (var work in SelectedRequest.WorkItems)
                {
                    workTable.Rows[workRow].Range.Font.Size = 11;
                    workTable.Rows[workRow].Range.Font.Name = "Times New Roman";
                    workTable.Rows[workRow].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    if (workRow % 2 == 0)
                        workTable.Rows[workRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                    workTable.Cell(workRow, 1).Range.Text = workNumber.ToString();
                    workTable.Cell(workRow, 2).Range.Text = work.ServiceName;
                    workTable.Cell(workRow, 3).Range.Text = work.EmployeeName;
                    workTable.Cell(workRow, 4).Range.Text = $"{work.Cost:N2} ₽";
                    workTable.Cell(workRow, 5).Range.Text = "Выполнено";
                    workTable.Cell(workRow, 5).Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                    workTable.Cell(workRow, 5).Range.Font.Bold = 1;

                    totalCost += work.Cost;
                    workRow++;
                    workNumber++;
                }

                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                // ====================== ИТОГОВАЯ СТРОКА ======================
                var totalPara = doc.Paragraphs.Add();
                totalPara.Range.InsertParagraphAfter();
                var totalTable = doc.Tables.Add(totalPara.Range, 1, 2);
                totalTable.Borders.Enable = 1;
                totalTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                totalTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                totalTable.Columns[1].Width = 350;
                totalTable.Columns[2].Width = 100;

                totalTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0xFDEDEC;
                totalTable.Rows[1].Range.Font.Bold = 1;
                totalTable.Rows[1].Range.Font.Size = 14;
                totalTable.Rows[1].Range.Font.Name = "Times New Roman";
                totalTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                totalTable.Cell(1, 1).Range.Text = "ИТОГО К ОПЛАТЕ:";
                totalTable.Cell(1, 2).Range.Text = $"{totalCost:N2} ₽";
                totalTable.Cell(1, 2).Range.Font.Color = Word.WdColor.wdColorDarkRed;
                totalTable.Cell(1, 2).Range.Font.Size = 16;

                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                // ====================== ПОДПИСИ ======================
                var signatures = doc.Paragraphs.Add();
                signatures.Range.Text = "___________________________________\n";
                signatures.Range.Font.Size = 11;
                signatures.Range.Font.Name = "Times New Roman";
                signatures.Range.Text += "        (Подпись мастера)\n\n";
                signatures.Range.Text += "___________________________________\n";
                signatures.Range.Text += "        (Подпись клиента)\n\n";
                signatures.Range.Text += $"__________________ {DateTime.Now:dd.MM.yyyy}\n";
                signatures.Range.Text += "        (Дата)";
                signatures.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

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
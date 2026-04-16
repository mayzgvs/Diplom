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

        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportRevenueToExcelCommand { get; private set; }
        public ICommand ExportRevenueToWordCommand { get; private set; }
        public ICommand ExportDetailedToExcelCommand { get; private set; }
        public ICommand ExportSingleRequestToExcelCommand { get; private set; }
        public ICommand ExportVehiclesReportCommand { get; private set; }
        public ICommand ExportEmployeesReportCommand { get; private set; }

        public ReportsViewModel()
        {
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

                CurrentRevenueReport = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта по выручке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetServiceNameById(int serviceId) => DbManager.GetServiceById(serviceId)?.Name ?? "Неизвестно";
        private string GetEmployeeNameById(int employeeId)
        {
            var employee = DbManager.GetEmployeeById(employeeId);
            return employee != null ? $"{employee.FirstName} {employee.LastName}" : "Неизвестно";
        }

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
                            Status = statusName
                        };

                        var workItems = DbManager.GetWorkItemsByRequestId(req.Id);
                        decimal calculatedTotalCost = 0;

                        if (workItems != null)
                        {
                            foreach (var wi in workItems)
                            {
                                var service = wi.ServiceId.HasValue ? DbManager.GetServiceById(wi.ServiceId.Value) : null;
                                var consumable = wi.ConsumableId.HasValue ? DbManager.GetConsumableById(wi.ConsumableId.Value) : null;
                                var employee = wi.EmployeeId.HasValue ? DbManager.GetEmployeeById(wi.EmployeeId.Value) : null;

                                string workName = "";
                                if (service != null)
                                {
                                    workName = service.Name;
                                }
                                else if (consumable != null)
                                {
                                    workName = consumable.Name;
                                }
                                else
                                {
                                    workName = ""; 
                                }

                                reportItem.WorkItems.Add(new ReportWorkItem
                                {
                                    ServiceName = workName, 
                                    EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Не назначен",
                                    Cost = wi.Cost
                                });

                                calculatedTotalCost += wi.Cost;
                            }
                        }

                        reportItem.TotalCost = calculatedTotalCost > 0 ? calculatedTotalCost : req.TotalCost;
                        _allRequests.Add(reportItem);
                    }
                }

                report.Requests = _allRequests;

                var completedRequests = report.Requests.Where(r => r.Status?.Trim() == "Завершённые" || r.Status?.Trim() == "Выдан клиенту" || r.Status?.Trim() == "Готов к выдаче").ToList();
                var totalRevenue = completedRequests.Sum(r => r.TotalCost);

                report.Summary = new ReportSummary
                {
                    TotalRequests = report.Requests.Count,
                    CompletedRequests = report.Requests.Count(r => r.Status?.Trim() == "Завершённые" || r.Status?.Trim() == "Выдан клиенту" || r.Status?.Trim() == "Готов к выдаче"),
                    InProgressRequests = report.Requests.Count(r => r.Status?.Trim() == "В процессе ремонта" || r.Status?.Trim() == "В работе"),
                    CancelledRequests = report.Requests.Count(r => r.Status?.Trim() == "Отменённые" || r.Status?.Trim() == "Новая заявка"),
                    TotalRevenue = totalRevenue,
                    AverageRequestValue = report.Requests.Count > 0 ? totalRevenue / report.Requests.Count : 0,
                    UniqueClients = report.Requests.Select(r => r.ClientName).Distinct().Count(),
                    UniqueCars = report.Requests.Select(r => r.CarInfo).Distinct().Count(),
                    TotalWorkItems = report.Requests.Sum(r => r.WorkItems.Count),
                    ActiveEmployees = report.Requests.SelectMany(r => r.WorkItems).Where(w => !string.IsNullOrEmpty(w.EmployeeName) && w.EmployeeName != "Не назначен").Select(w => w.EmployeeName).Distinct().Count()
                };

                CurrentDetailedReport = report;
                FilterDetailedReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки детального отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterDetailedReport()
        {
            if (_allRequests == null) return;
            FilteredRequests.Clear();
            var filtered = _allRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(r => r.RequestId.ToString().Contains(SearchText) || r.ClientName.ToLower().Contains(SearchText.ToLower()) || r.CarInfo.ToLower().Contains(SearchText.ToLower()));
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

        private void ExportRevenueReportToExcel()
        {
            try
            {
                if (CurrentRevenueReport == null) return;
                var dialog = new SaveFileDialog { Filter = "Excel файлы (*.xlsx)|*.xlsx", FileName = $"Отчёт_по_выручке_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
                if (dialog.ShowDialog() != true) return;
                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Отчёт по выручке");
                    ws.Cells["A1"].Value = "ОТЧЁТ ПО ВЫРУЧКЕ АВТОСЕРВИСА";
                    ws.Cells["A1:D1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Size = 20;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells["A2"].Value = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
                    ws.Cells["A2:D2"].Merge = true;
                    ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells["A3"].Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cells["A3:D3"].Merge = true;
                    ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    int row = 5;
                    ws.Cells[$"A{row}"].Value = "КЛЮЧЕВЫЕ ПОКАЗАТЕЛИ";
                    ws.Cells[$"A{row}:D{row}"].Merge = true;
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
                        ws.Cells[$"A{row}"].Value = "№";
                        ws.Cells[$"B{row}"].Value = "Услуга";
                        ws.Cells[$"C{row}"].Value = "Кол-во";
                        ws.Cells[$"D{row}"].Value = "Выручка";
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
                        ws.Cells[$"A{row}"].Value = "№";
                        ws.Cells[$"B{row}"].Value = "Сотрудник";
                        ws.Cells[$"C{row}"].Value = "Работ";
                        ws.Cells[$"D{row}"].Value = "Выручка";
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
                        row += 2;
                    }

                    row += 4;
                    ws.Cells[$"A{row}"].Value = "_________________________";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Руководитель";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = $"\"___\" ______________ 20___ г.";
                    ws.Cells[$"B{row}"].Value = "М.П.";
                    ws.Cells[$"B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 9;

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }
                MessageBox.Show("Отчёт по выручке успешно сохранён в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportRevenueReportToWord()
        {
            try
            {
                if (CurrentRevenueReport == null) return;
                var dialog = new SaveFileDialog { Filter = "Word документы (*.docx)|*.docx", FileName = $"Отчёт_по_выручке_{DateTime.Now:yyyyMMdd_HHmmss}.docx" };
                if (dialog.ShowDialog() != true) return;

                var wordApp = new Word.Application { Visible = false };
                var doc = wordApp.Documents.Add();
                doc.PageSetup.Orientation = Word.WdOrientation.wdOrientLandscape;
                doc.PageSetup.LeftMargin = 40f;
                doc.PageSetup.RightMargin = 40f;
                doc.PageSetup.TopMargin = 40f;
                doc.PageSetup.BottomMargin = 40f;

                var title = doc.Paragraphs.Add();
                var titleRange = title.Range;
                titleRange.Text = "ОТЧЁТ ПО ВЫРУЧКЕ АВТОСЕРВИСА";
                titleRange.Font.Size = 24;
                titleRange.Font.Bold = 1;
                titleRange.Font.Name = "Times New Roman";
                titleRange.Font.Color = Word.WdColor.wdColorDarkBlue;
                titleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                titleRange.ParagraphFormat.SpaceAfter = 8;
                titleRange.InsertParagraphAfter();

                var period = doc.Paragraphs.Add();
                var periodRange = period.Range;
                periodRange.Text = $"Период: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
                periodRange.Font.Size = 12;
                periodRange.Font.Name = "Times New Roman";
                periodRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                periodRange.ParagraphFormat.SpaceAfter = 4;
                periodRange.InsertParagraphAfter();

                var dateGen = doc.Paragraphs.Add();
                var dateRange = dateGen.Range;
                dateRange.Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                dateRange.Font.Size = 10;
                dateRange.Font.Name = "Times New Roman";
                dateRange.Font.Italic = 1;
                dateRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                dateRange.ParagraphFormat.SpaceAfter = 16;
                dateRange.InsertParagraphAfter();

                var metricsTitle = doc.Paragraphs.Add();
                var metricsTitleRange = metricsTitle.Range;
                metricsTitleRange.Text = "КЛЮЧЕВЫЕ ПОКАЗАТЕЛИ";
                metricsTitleRange.Font.Size = 14;
                metricsTitleRange.Font.Bold = 1;
                metricsTitleRange.Font.Name = "Times New Roman";
                metricsTitleRange.Font.Color = Word.WdColor.wdColorDarkBlue;
                metricsTitleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTitleRange.ParagraphFormat.SpaceAfter = 8;
                metricsTitleRange.InsertParagraphAfter();

                var metricsTable = doc.Tables.Add(doc.Paragraphs.Last.Range, 2, 4);
                metricsTable.Borders.Enable = 1;
                metricsTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                metricsTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                metricsTable.AllowPageBreaks = false;

                metricsTable.Columns[1].Width = 90;
                metricsTable.Columns[2].Width = 80;
                metricsTable.Columns[3].Width = 80;
                metricsTable.Columns[4].Width = 90;

                metricsTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x2C3E50;
                metricsTable.Rows[1].Range.Font.Bold = 1;
                metricsTable.Rows[1].Range.Font.Size = 11;
                metricsTable.Rows[1].Range.Font.Name = "Times New Roman";
                metricsTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                metricsTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTable.Cell(1, 1).Range.Text = "Общая выручка";
                metricsTable.Cell(1, 2).Range.Text = "Всего заявок";
                metricsTable.Cell(1, 3).Range.Text = "Средний чек";
                metricsTable.Cell(1, 4).Range.Text = "Выручка в день";

                metricsTable.Rows[2].Range.Font.Size = 12;
                metricsTable.Rows[2].Range.Font.Bold = 1;
                metricsTable.Rows[2].Range.Font.Name = "Times New Roman";
                metricsTable.Rows[2].Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                metricsTable.Rows[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTable.Rows[2].Shading.BackgroundPatternColor = (Word.WdColor)0xE8F4FD;
                metricsTable.Cell(2, 1).Range.Text = $"{CurrentRevenueReport.TotalRevenue:N2} ₽";
                metricsTable.Cell(2, 2).Range.Text = CurrentRevenueReport.TotalRequests.ToString();
                metricsTable.Cell(2, 3).Range.Text = $"{CurrentRevenueReport.AverageRequestValue:N2} ₽";
                metricsTable.Cell(2, 4).Range.Text = $"{CurrentRevenueReport.AverageRevenuePerDay:N2} ₽";

                metricsTable.Rows[1].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                metricsTable.Rows[2].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                doc.Paragraphs.Last.Range.InsertParagraphAfter();
                doc.Paragraphs.Last.Range.ParagraphFormat.SpaceAfter = 8;
                doc.Paragraphs.Last.Range.InsertParagraphAfter();

                if (CurrentRevenueReport.TopServices.Any())
                {
                    var servicesTitle = doc.Paragraphs.Add();
                    var servicesTitleRange = servicesTitle.Range;
                    servicesTitleRange.Text = "ТОП-5 УСЛУГ ПО ВЫРУЧКЕ";
                    servicesTitleRange.Font.Size = 14;
                    servicesTitleRange.Font.Bold = 1;
                    servicesTitleRange.Font.Name = "Times New Roman";
                    servicesTitleRange.Font.Color = Word.WdColor.wdColorDarkBlue;
                    servicesTitleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    servicesTitleRange.ParagraphFormat.SpaceAfter = 8;
                    servicesTitleRange.InsertParagraphAfter();

                    var servicesTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.TopServices.Count + 1, 4);
                    servicesTable.Borders.Enable = 1;
                    servicesTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    servicesTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    servicesTable.Columns[1].Width = 35;
                    servicesTable.Columns[2].Width = 200;
                    servicesTable.Columns[3].Width = 60;
                    servicesTable.Columns[4].Width = 100;

                    servicesTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0x27AE60;
                    servicesTable.Rows[1].Range.Font.Bold = 1;
                    servicesTable.Rows[1].Range.Font.Size = 11;
                    servicesTable.Rows[1].Range.Font.Name = "Times New Roman";
                    servicesTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    servicesTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    servicesTable.Cell(1, 1).Range.Text = "№";
                    servicesTable.Cell(1, 2).Range.Text = "Услуга";
                    servicesTable.Cell(1, 3).Range.Text = "Кол-во";
                    servicesTable.Cell(1, 4).Range.Text = "Выручка, ₽";

                    int serviceRow = 2;
                    foreach (var service in CurrentRevenueReport.TopServices)
                    {
                        servicesTable.Rows[serviceRow].Range.Font.Size = 10;
                        servicesTable.Rows[serviceRow].Range.Font.Name = "Times New Roman";
                        servicesTable.Rows[serviceRow].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                        if (serviceRow % 2 == 0)
                            servicesTable.Rows[serviceRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        servicesTable.Cell(serviceRow, 1).Range.Text = service.Index.ToString();
                        servicesTable.Cell(serviceRow, 2).Range.Text = service.ServiceName;
                        servicesTable.Cell(serviceRow, 3).Range.Text = service.Count.ToString();
                        servicesTable.Cell(serviceRow, 4).Range.Text = $"{service.Revenue:N2}";
                        servicesTable.Cell(serviceRow, 4).Range.Font.Bold = 1;
                        servicesTable.Cell(serviceRow, 4).Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                        serviceRow++;
                    }

                    servicesTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                    doc.Paragraphs.Last.Range.ParagraphFormat.SpaceAfter = 8;
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                }

                if (CurrentRevenueReport.TopEmployees.Any())
                {
                    var breakPara = doc.Paragraphs.Add();
                    var breakRange = breakPara.Range;
                    breakRange.InsertBreak(Word.WdBreakType.wdPageBreak);
                    breakRange.InsertParagraphAfter();

                    var employeesTitle = doc.Paragraphs.Add();
                    var employeesTitleRange = employeesTitle.Range;
                    employeesTitleRange.Text = "ТОП-5 СОТРУДНИКОВ ПО ВЫРУЧКЕ";
                    employeesTitleRange.Font.Size = 14;
                    employeesTitleRange.Font.Bold = 1;
                    employeesTitleRange.Font.Name = "Times New Roman";
                    employeesTitleRange.Font.Color = Word.WdColor.wdColorDarkBlue;
                    employeesTitleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    employeesTitleRange.ParagraphFormat.SpaceAfter = 8;
                    employeesTitleRange.InsertParagraphAfter();

                    var employeesTable = doc.Tables.Add(doc.Paragraphs.Last.Range, CurrentRevenueReport.TopEmployees.Count + 1, 5);
                    employeesTable.Borders.Enable = 1;
                    employeesTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    employeesTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    employeesTable.Columns[1].Width = 35;
                    employeesTable.Columns[2].Width = 160;
                    employeesTable.Columns[3].Width = 70;
                    employeesTable.Columns[4].Width = 80;
                    employeesTable.Columns[5].Width = 90;

                    employeesTable.Rows[1].Shading.BackgroundPatternColor = (Word.WdColor)0xE67E22;
                    employeesTable.Rows[1].Range.Font.Bold = 1;
                    employeesTable.Rows[1].Range.Font.Size = 11;
                    employeesTable.Rows[1].Range.Font.Name = "Times New Roman";
                    employeesTable.Rows[1].Range.Font.Color = Word.WdColor.wdColorWhite;
                    employeesTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    employeesTable.Cell(1, 1).Range.Text = "№";
                    employeesTable.Cell(1, 2).Range.Text = "Сотрудник";
                    employeesTable.Cell(1, 3).Range.Text = "Выполнено работ";
                    employeesTable.Cell(1, 4).Range.Text = "Средний чек, ₽";
                    employeesTable.Cell(1, 5).Range.Text = "Выручка, ₽";

                    int empRow = 2;
                    foreach (var emp in CurrentRevenueReport.TopEmployees)
                    {
                        employeesTable.Rows[empRow].Range.Font.Size = 10;
                        employeesTable.Rows[empRow].Range.Font.Name = "Times New Roman";
                        employeesTable.Rows[empRow].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                        if (empRow % 2 == 0)
                            employeesTable.Rows[empRow].Shading.BackgroundPatternColor = (Word.WdColor)0xF5F5F5;

                        employeesTable.Cell(empRow, 1).Range.Text = emp.Index.ToString();
                        employeesTable.Cell(empRow, 2).Range.Text = emp.EmployeeName;
                        employeesTable.Cell(empRow, 3).Range.Text = emp.CompletedJobs.ToString();
                        employeesTable.Cell(empRow, 4).Range.Text = $"{emp.AverageJobValue:N2}";
                        employeesTable.Cell(empRow, 5).Range.Text = $"{emp.TotalRevenue:N2}";
                        employeesTable.Cell(empRow, 5).Range.Font.Bold = 1;
                        employeesTable.Cell(empRow, 5).Range.Font.Color = Word.WdColor.wdColorDarkGreen;
                        empRow++;
                    }

                    employeesTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                    doc.Paragraphs.Last.Range.ParagraphFormat.SpaceAfter = 12;
                    doc.Paragraphs.Last.Range.InsertParagraphAfter();
                }

                var signaturesTable = doc.Tables.Add(doc.Paragraphs.Last.Range, 3, 2);
                signaturesTable.Borders.Enable = 0;
                signaturesTable.AllowPageBreaks = false;

                signaturesTable.Columns[1].Width = 250;
                signaturesTable.Columns[2].Width = 250;

                signaturesTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                signaturesTable.Rows[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                signaturesTable.Rows[3].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                signaturesTable.Cell(1, 1).Range.Text = "_________________________";
                signaturesTable.Cell(1, 1).Range.Font.Size = 10;
                signaturesTable.Cell(1, 2).Range.Font.Size = 10;
                signaturesTable.Cell(1, 1).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(1, 2).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(1, 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                signaturesTable.Cell(1, 2).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                signaturesTable.Cell(2, 1).Range.Text = "Руководитель";
                signaturesTable.Cell(2, 1).Range.Font.Size = 10;
                signaturesTable.Cell(2, 2).Range.Font.Size = 10;
                signaturesTable.Cell(2, 1).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(2, 2).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(2, 1).Range.Font.Bold = 1;
                signaturesTable.Cell(2, 2).Range.Font.Bold = 1;
                signaturesTable.Cell(2, 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                signaturesTable.Cell(2, 2).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                signaturesTable.Cell(3, 1).Range.Text = $"\"___\" ______________ 20___ г.";
                signaturesTable.Cell(3, 2).Range.Text = "М.П.";
                signaturesTable.Cell(3, 1).Range.Font.Size = 9;
                signaturesTable.Cell(3, 2).Range.Font.Size = 10;
                signaturesTable.Cell(3, 1).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(3, 2).Range.Font.Name = "Times New Roman";
                signaturesTable.Cell(3, 2).Range.Font.Bold = 1;
                signaturesTable.Cell(3, 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                signaturesTable.Cell(3, 2).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                doc.SaveAs2(dialog.FileName);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show("Отчёт по выручке успешно сохранён в Word!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}\n\nУбедитесь, что установлен Microsoft Word", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var dialog = new SaveFileDialog { Filter = "Excel файлы (*.xlsx)|*.xlsx", FileName = $"Детальный_отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
                if (dialog.ShowDialog() != true) return;
                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Детальный отчёт");

                    ws.Column(1).Width = 8;  
                    ws.Column(2).Width = 25;  
                    ws.Column(3).Width = 30; 
                    ws.Column(4).Width = 12; 
                    ws.Column(5).Width = 18;  
                    ws.Column(6).Width = 14; 
                    ws.Column(7).Width = 8;   
                    ws.Column(8).Width = 45;  

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

                    ws.Cells[$"A{row}"].Value = "СВОДКА";
                    ws.Cells[$"A{row}:H{row}"].Merge = true;
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}"].Style.Font.Size = 14;
                    ws.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 152, 219));
                    ws.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
                    row += 2;

                    int summaryStartRow = row;
                    AddMetric(ws, ref row, "Всего заявок:", CurrentDetailedReport.Summary.TotalRequests, "0");
                    AddMetric(ws, ref row, "Завершённые:", CurrentDetailedReport.Summary.CompletedRequests, "0");
                    AddMetric(ws, ref row, "В работе:", CurrentDetailedReport.Summary.InProgressRequests, "0");
                    AddMetric(ws, ref row, "Отменённые:", CurrentDetailedReport.Summary.CancelledRequests, "0");
                    AddMetric(ws, ref row, "Общая выручка:", CurrentDetailedReport.Summary.TotalRevenue, "#,##0.00 ₽");
                    AddMetric(ws, ref row, "Средний чек:", CurrentDetailedReport.Summary.AverageRequestValue, "#,##0.00 ₽");

                    int metricRow = summaryStartRow;
                    ws.Cells[$"D{metricRow}"].Value = "Уникальных клиентов:";
                    ws.Cells[$"D{metricRow}"].Style.Font.Bold = true;
                    ws.Cells[$"E{metricRow}"].Value = CurrentDetailedReport.Summary.UniqueClients;
                    metricRow++;

                    ws.Cells[$"D{metricRow}"].Value = "Уникальных авто:";
                    ws.Cells[$"D{metricRow}"].Style.Font.Bold = true;
                    ws.Cells[$"E{metricRow}"].Value = CurrentDetailedReport.Summary.UniqueCars;
                    metricRow++;

                    ws.Cells[$"D{metricRow}"].Value = "Всего работ:";
                    ws.Cells[$"D{metricRow}"].Style.Font.Bold = true;
                    ws.Cells[$"E{metricRow}"].Value = CurrentDetailedReport.Summary.TotalWorkItems;
                    metricRow++;

                    ws.Cells[$"D{metricRow}"].Value = "Активных сотрудников:";
                    ws.Cells[$"D{metricRow}"].Style.Font.Bold = true;
                    ws.Cells[$"E{metricRow}"].Value = CurrentDetailedReport.Summary.ActiveEmployees;

                    row = Math.Max(row, metricRow + 2);

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
                    ws.Cells[$"G{row}"].Value = "Работ";
                    ws.Cells[$"H{row}"].Value = "Выполненные работы";

                    ws.Cells[$"A{row}:H{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:H{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"A{row}:H{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 73, 94));
                    ws.Cells[$"A{row}:H{row}"].Style.Font.Color.SetColor(Color.White);
                    ws.Cells[$"A{row}:H{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[$"A{row}:H{row}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    row += 1;

                    int displayRow = row;
                    foreach (var req in CurrentDetailedReport.Requests)
                    {
                        ws.Cells[$"A{displayRow}"].Value = req.RequestId;
                        ws.Cells[$"A{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        ws.Cells[$"A{displayRow}"].Style.Font.Bold = true;

                        ws.Cells[$"B{displayRow}"].Value = req.ClientName;

                        ws.Cells[$"C{displayRow}"].Value = req.CarInfo;

                        ws.Cells[$"D{displayRow}"].Value = req.StartDate.ToString("dd.MM.yyyy");
                        ws.Cells[$"D{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells[$"E{displayRow}"].Value = req.Status;
                        ws.Cells[$"E{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        if (req.Status == "Завершённые" || req.Status == "Выдан клиенту" || req.Status == "Готов к выдаче")
                        {
                            ws.Cells[$"E{displayRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[$"E{displayRow}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(212, 237, 218));
                            ws.Cells[$"E{displayRow}"].Style.Font.Color.SetColor(Color.FromArgb(40, 167, 69));
                        }
                        else if (req.Status == "В процессе ремонта" || req.Status == "В работе")
                        {
                            ws.Cells[$"E{displayRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[$"E{displayRow}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 243, 205));
                            ws.Cells[$"E{displayRow}"].Style.Font.Color.SetColor(Color.FromArgb(255, 193, 7));
                        }
                        else if (req.Status == "Новая заявка")
                        {
                            ws.Cells[$"E{displayRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[$"E{displayRow}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(209, 231, 221));
                            ws.Cells[$"E{displayRow}"].Style.Font.Color.SetColor(Color.FromArgb(23, 162, 184));
                        }
                        else if (req.Status == "Отменённые")
                        {
                            ws.Cells[$"E{displayRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[$"E{displayRow}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 215, 218));
                            ws.Cells[$"E{displayRow}"].Style.Font.Color.SetColor(Color.FromArgb(220, 53, 69));
                        }

                        ws.Cells[$"F{displayRow}"].Value = req.TotalCost;
                        ws.Cells[$"F{displayRow}"].Style.Numberformat.Format = "#,##0.00 ₽";
                        ws.Cells[$"F{displayRow}"].Style.Font.Bold = true;
                        ws.Cells[$"F{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[$"G{displayRow}"].Value = req.WorkItems.Count;
                        ws.Cells[$"G{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        var worksList = new List<string>();
                        int workNum = 1;
                        foreach (var w in req.WorkItems)
                        {
                            worksList.Add($"{workNum}. {w.ServiceName} — {w.EmployeeName}: {w.Cost:N2} ₽");
                            workNum++;
                        }
                        ws.Cells[$"H{displayRow}"].Value = string.Join(Environment.NewLine, worksList);
                        ws.Cells[$"H{displayRow}"].Style.WrapText = true;
                        ws.Cells[$"H{displayRow}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                        displayRow++;
                    }

                    ws.Cells[$"E{displayRow}"].Value = "ИТОГО:";
                    ws.Cells[$"E{displayRow}"].Style.Font.Bold = true;
                    ws.Cells[$"E{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    ws.Cells[$"F{displayRow}"].Value = CurrentDetailedReport.Summary.TotalRevenue;
                    ws.Cells[$"F{displayRow}"].Style.Numberformat.Format = "#,##0.00 ₽";
                    ws.Cells[$"F{displayRow}"].Style.Font.Bold = true;
                    ws.Cells[$"F{displayRow}"].Style.Font.Color.SetColor(Color.FromArgb(220, 53, 69));
                    ws.Cells[$"F{displayRow}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    ws.Cells[$"E{displayRow}:F{displayRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[$"E{displayRow}:F{displayRow}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 215, 218));

                    displayRow += 3;

                    ws.Cells[$"A{displayRow}"].Value = "_________________________";
                    ws.Cells[$"A{displayRow}:B{displayRow}"].Style.Font.Size = 10;
                    displayRow++;

                    ws.Cells[$"A{displayRow}"].Value = "Руководитель";
                    ws.Cells[$"A{displayRow}:B{displayRow}"].Style.Font.Bold = true;
                    ws.Cells[$"A{displayRow}:B{displayRow}"].Style.Font.Size = 10;
                    displayRow++;

                    ws.Cells[$"A{displayRow}"].Value = $"\"___\" ______________ 20___ г.";
                    ws.Cells[$"B{displayRow}"].Value = "М.П.";
                    ws.Cells[$"B{displayRow}"].Style.Font.Bold = true;
                    ws.Cells[$"A{displayRow}:B{displayRow}"].Style.Font.Size = 9;

                    var dataRange = ws.Cells[$"A{row}:H{displayRow - 4}"];
                    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    for (int i = row + 1; i < displayRow - 3; i++)
                    {
                        if (i % 2 == 0)
                        {
                            ws.Cells[$"A{i}:H{i}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[$"A{i}:H{i}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                        }
                    }

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }
                MessageBox.Show("Детальный отчёт успешно сохранён в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportVehiclesReportToExcel()
        {
            try
            {
                var dialog = new SaveFileDialog { Filter = "Excel файлы (*.xlsx)|*.xlsx", FileName = $"Отчёт_по_автомобилям_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
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
                    var carStats = cars.GroupBy(c => c.Brand).Select(g => new { Brand = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToList();
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
                    row += 3;

                    ws.Cells[$"A{row}"].Value = "_________________________";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Руководитель";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = $"\"___\" ______________ 20___ г.";
                    ws.Cells[$"B{row}"].Value = "М.П.";
                    ws.Cells[$"B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 9;

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }
                MessageBox.Show("Отчёт по автомобилям успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportEmployeesReportToExcel()
        {
            try
            {
                var dialog = new SaveFileDialog { Filter = "Excel файлы (*.xlsx)|*.xlsx", FileName = $"Отчёт_по_сотрудникам_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
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
                    var employeeStats = workItems.Where(wi => wi.EmployeeId.HasValue).GroupBy(wi => wi.EmployeeId.Value).Select(g => new
                    {
                        EmployeeId = g.Key,
                        EmployeeName = GetEmployeeNameById(g.Key),
                        CompletedJobs = g.Count(),
                        TotalRevenue = g.Sum(wi => wi.Cost)
                    }).OrderByDescending(x => x.TotalRevenue).ToList();
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
                    row += 3;

                    ws.Cells[$"A{row}"].Value = "_________________________";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Руководитель";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = $"\"___\" ______________ 20___ г.";
                    ws.Cells[$"B{row}"].Value = "М.П.";
                    ws.Cells[$"B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 9;

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }
                MessageBox.Show("Отчёт по сотрудникам успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSingleRequestToExcel()
        {
            try
            {
                if (SelectedRequest == null) return;
                var dialog = new SaveFileDialog { Filter = "Excel файлы (*.xlsx)|*.xlsx", FileName = $"Заказ-наряд_{SelectedRequest.RequestId}_{DateTime.Now:yyyyMMdd}.xlsx" };
                if (dialog.ShowDialog() != true) return;
                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Заказ-наряд");
                    ws.Cells["A1"].Value = "АВТОСЕРВИС";
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
                    ws.Cells[$"A{row}"].Value = "Клиент:";
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.ClientName;
                    ws.Cells[$"B{row}:E{row}"].Merge = true;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Автомобиль:";
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.CarInfo;
                    ws.Cells[$"B{row}:E{row}"].Merge = true;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Статус:";
                    ws.Cells[$"A{row}"].Style.Font.Bold = true;
                    ws.Cells[$"B{row}"].Value = SelectedRequest.Status;
                    ws.Cells[$"B{row}:E{row}"].Merge = true;
                    row += 2;
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
                    row += 3;

                    ws.Cells[$"A{row}"].Value = "_________________________";
                    ws.Cells[$"B{row}"].Value = "_________________________";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = "Руководитель";
                    ws.Cells[$"B{row}"].Value = "Клиент";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 10;
                    row++;
                    ws.Cells[$"A{row}"].Value = $"\"___\" ______________ 20___ г.";
                    ws.Cells[$"A{row}:B{row}"].Style.Font.Size = 9;

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }
                MessageBox.Show("Заказ-наряд успешно сохранён в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
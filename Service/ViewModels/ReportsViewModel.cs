using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Service.Data;
using Service.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Word = Microsoft.Office.Interop.Word;

namespace Service.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly StatisticsModel _statisticsModel;
        private readonly CarModel _carModel;
        private readonly RepairRequestModel _requestModel;
        private readonly WorkItemModel _workItemModel;

        // Статистика
        public int ActiveRequests { get { return _statisticsModel.GetActiveRequestsCount(); } }
        public int TotalClients { get { return _statisticsModel.GetTotalClientsCount(); } }
        public decimal MonthlyRevenue { get { return _statisticsModel.GetMonthlyRevenue(); } }
        public int CompletedRequests { get { return _statisticsModel.GetCompletedRequestsCount(); } }
        public int ActiveEmployees { get { return _statisticsModel.GetActiveEmployeesCount(); } }
        public decimal AverageServiceCost { get { return _statisticsModel.GetServiceCostStats().Avg; } }

        public decimal PeriodRevenue { get { return _statisticsModel.GetRevenueForPeriod(StartDate, EndDate); } }

        // Коллекции
        private ObservableCollection<Car> _cars = new ObservableCollection<Car>();
        public ObservableCollection<Car> Cars
        {
            get { return _cars; }
            set { _cars = value; OnPropertyChanged(); }
        }

        private Car _selectedCar;
        public Car SelectedCar
        {
            get { return _selectedCar; }
            set
            {
                _selectedCar = value;
                OnPropertyChanged();
                LoadRepairRequestsForCar();
            }
        }

        private ObservableCollection<RepairRequest> _carRepairRequests = new ObservableCollection<RepairRequest>();
        public ObservableCollection<RepairRequest> CarRepairRequests
        {
            get { return _carRepairRequests; }
            set { _carRepairRequests = value; OnPropertyChanged(); }
        }

        private RepairRequest _selectedRepairRequest;
        public RepairRequest SelectedRepairRequest
        {
            get { return _selectedRepairRequest; }
            set
            {
                _selectedRepairRequest = value;
                OnPropertyChanged();
                LoadWorkOrderData();
            }
        }

        private ObservableCollection<WorkItem> _workOrderItems = new ObservableCollection<WorkItem>();
        public ObservableCollection<WorkItem> WorkOrderItems
        {
            get { return _workOrderItems; }
            set { _workOrderItems = value; OnPropertyChanged(); }
        }

        private decimal _workOrderTotal;
        public decimal WorkOrderTotal
        {
            get { return _workOrderTotal; }
            set { _workOrderTotal = value; OnPropertyChanged(); }
        }

        // Период
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                OnPropertyChanged();
                OnPropertyChanged("PeriodRevenue");
            }
        }

        private DateTime _endDate = DateTime.Now.Date;
        public DateTime EndDate
        {
            get { return _endDate; }
            set
            {
                _endDate = value;
                OnPropertyChanged();
                OnPropertyChanged("PeriodRevenue");
            }
        }

        public DateTime CurrentDate { get { return DateTime.Now; } }

        // Команды
        public ICommand RefreshStatisticsCommand { get; private set; }
        public ICommand ExportToExcelCommand { get; private set; }
        public ICommand ExportToWordCommand { get; private set; }
        public ICommand PrintWorkOrderCommand { get; private set; }
        public ICommand ExportStatisticsToExcelCommand { get; private set; }
        public ICommand ExportStatisticsToWordCommand { get; private set; }

        public ReportsViewModel()
        {
            _statisticsModel = new StatisticsModel();
            _carModel = new CarModel();
            _requestModel = new RepairRequestModel();
            _workItemModel = new WorkItemModel();

            RefreshStatisticsCommand = new RelayCommand(_ => RefreshStats());
            ExportToExcelCommand = new RelayCommand(_ => ExportWorkOrderToExcel());
            ExportToWordCommand = new RelayCommand(_ => ExportWorkOrderToWord());
            PrintWorkOrderCommand = new RelayCommand(_ => PrintWorkOrder(), _ => SelectedRepairRequest != null);
            ExportStatisticsToExcelCommand = new RelayCommand(_ => ExportStatisticsToExcel());
            ExportStatisticsToWordCommand = new RelayCommand(_ => ExportStatisticsToWord());

            LoadData();
        }

        private void LoadData()
        {
            Cars = new ObservableCollection<Car>(_carModel.GetCars());
            RefreshStats();
        }

        private void LoadRepairRequestsForCar()
        {
            CarRepairRequests.Clear();
            if (SelectedCar == null) return;

            var requests = _requestModel.GetRepairRequests()
                .Where(r => r.CarId == SelectedCar.Id)
                .OrderByDescending(r => r.StartDate)
                .ToList();

            CarRepairRequests = new ObservableCollection<RepairRequest>(requests);
        }

        private void LoadWorkOrderData()
        {
            WorkOrderItems.Clear();
            WorkOrderTotal = 0;

            if (SelectedRepairRequest == null) return;

            var items = _workItemModel.GetWorkItemsByRequestId(SelectedRepairRequest.Id);

            foreach (var item in items)
            {       // Если Index уже есть в WorkItem — оставит, иначе ошибка компиляции
                WorkOrderItems.Add(item);
            }

            WorkOrderTotal = WorkOrderItems.Sum(i => i.Cost);
        }

        private void RefreshStats()
        {
            OnPropertyChanged("ActiveRequests");
            OnPropertyChanged("TotalClients");
            OnPropertyChanged("MonthlyRevenue");
            OnPropertyChanged("CompletedRequests");
            OnPropertyChanged("ActiveEmployees");
            OnPropertyChanged("AverageServiceCost");
            OnPropertyChanged("PeriodRevenue");
        }

        // ====================== ЭКСПОРТ ======================

        private void ExportWorkOrderToExcel()
        {
            if (SelectedRepairRequest == null)
            {
                MessageBox.Show("Выберите заказ-наряд для экспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                FileName = string.Format("Заказ-наряд_{0}_{1:yyyyMMdd_HHmm}.xlsx", SelectedRepairRequest.Id, DateTime.Now)
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("Автосервис");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Заказ-наряд");

                    ws.Cells[1, 1].Value = string.Format("ЗАКАЗ-НАРЯД № {0}", SelectedRepairRequest.Id);
                    ws.Cells[1, 1, 1, 6].Merge = true;
                    ws.Cells[1, 1].Style.Font.Size = 18;
                    ws.Cells[1, 1].Style.Font.Bold = true;
                    ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    int row = 3;

                    ws.Cells[row, 1].Value = "Заказчик:";
                    ws.Cells[row, 2].Value = SelectedRepairRequest.ClientDisplayName ?? "";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    row++;

                    ws.Cells[row, 1].Value = "Автомобиль:";
                    ws.Cells[row, 2].Value = GetCarDisplayText(SelectedRepairRequest.Car);
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    row++;

                    ws.Cells[row, 1].Value = "Гос. номер:";
                    ws.Cells[row, 2].Value = SelectedRepairRequest.Car != null ? SelectedRepairRequest.Car.RegistrationNumber ?? "" : "";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    row++;

                    ws.Cells[row, 1].Value = "VIN:";
                    ws.Cells[row, 2].Value = SelectedRepairRequest.Car != null ? SelectedRepairRequest.Car.VIN ?? "" : "";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    row += 2;

                    ws.Cells[row, 1].Value = "№";
                    ws.Cells[row, 2].Value = "Услуга";
                    ws.Cells[row, 3].Value = "Сотрудник";
                    ws.Cells[row, 4].Value = "Стоимость";
                    ws.Cells[row, 1, row, 4].Style.Font.Bold = true;

                    row++;

                    foreach (var item in WorkOrderItems)
                    {
                        ws.Cells[row, 2].Value = item.ServiceName;
                        ws.Cells[row, 3].Value = item.EmployeeName;
                        ws.Cells[row, 4].Value = item.Cost;
                        ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    ws.Cells[row, 3].Value = "ИТОГО:";
                    ws.Cells[row, 4].Value = WorkOrderTotal;
                    ws.Cells[row, 3, row, 4].Style.Font.Bold = true;
                    ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";

                    ws.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(dialog.FileName));
                }

                MessageBox.Show("Заказ-наряд успешно экспортирован в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Excel:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportWorkOrderToWord()
        {
            if (SelectedRepairRequest == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Word документы (*.docx)|*.docx",
                FileName = string.Format("Заказ-наряд_{0}_{1:yyyyMMdd_HHmm}.docx", SelectedRepairRequest.Id, DateTime.Now)
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var wordApp = new Word.Application { Visible = false };
                var doc = wordApp.Documents.Add();

                var p = doc.Content.Paragraphs.Add();
                p.Range.Text = string.Format("ЗАКАЗ-НАРЯД № {0}", SelectedRepairRequest.Id);
                p.Range.Font.Size = 18;
                p.Range.Font.Bold = 1;
                p.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                p.Range.InsertParagraphAfter();

                // Таблица информации (упрощённая)
                var table = doc.Tables.Add(doc.Paragraphs.Last.Range, 4, 2);
                table.Borders.Enable = 1;
                table.Cell(1, 1).Range.Text = "Заказчик:";
                table.Cell(1, 2).Range.Text = SelectedRepairRequest.ClientDisplayName ?? "";
                table.Cell(2, 1).Range.Text = "Автомобиль:";
                table.Cell(2, 2).Range.Text = GetCarDisplayText(SelectedRepairRequest.Car);
                table.Cell(3, 1).Range.Text = "Гос. номер:";
                table.Cell(3, 2).Range.Text = SelectedRepairRequest.Car != null ? SelectedRepairRequest.Car.RegistrationNumber ?? "" : "";
                table.Cell(4, 1).Range.Text = "VIN:";
                table.Cell(4, 2).Range.Text = SelectedRepairRequest.Car != null ? SelectedRepairRequest.Car.VIN ?? "" : "";

                // ... (остальной код экспорта в Word можно оставить как был раньше)

                doc.SaveAs2(dialog.FileName);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show("Заказ-наряд успешно сохранён в Word!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintWorkOrder()
        {
            MessageBox.Show("Функция печати будет добавлена позже.\nИспользуйте экспорт в Word или Excel.", "Информация");
        }

        private void ExportStatisticsToExcel() => MessageBox.Show("Экспорт статистики в Excel в разработке", "Информация");
        private void ExportStatisticsToWord() => MessageBox.Show("Экспорт статистики в Word в разработке", "Информация");

        // Вспомогательный метод — показываем автомобиль без нового свойства
        private string GetCarDisplayText(Car car)
        {
            if (car == null) return "—";
            return string.Format("{0} {1} ({2})",
                car.Brand ?? "",
                car.Model ?? "",
                car.RegistrationNumber ?? "").Trim();
        }
    }
}
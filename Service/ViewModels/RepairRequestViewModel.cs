using Service.Data;
using Service.Models;
using Service.Services;
using Service.Utility;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class RepairRequestViewModel : BaseViewModel
    {
        private readonly RepairRequestModel _model = new RepairRequestModel();
        private readonly NotificationService _notificationService = new NotificationService();

        public ObservableCollection<RepairRequest> RepairRequests { get; private set; }
        public ObservableCollection<RepairRequest> FilteredRequests { get; private set; }
        public ObservableCollection<StatusRequest> Statuses { get; private set; }

        public ObservableCollection<StatusFilterItem> FilterStatuses { get; private set; }

        private RepairRequest _selectedRepairRequest;
        public RepairRequest SelectedRepairRequest
        {
            get => _selectedRepairRequest;
            set { _selectedRepairRequest = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                Filter();
            }
        }

        private StatusFilterItem _selectedFilterStatus;
        public StatusFilterItem SelectedFilterStatus
        {
            get => _selectedFilterStatus;
            set
            {
                _selectedFilterStatus = value;
                OnPropertyChanged();
                Filter();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ChangeStatusCommand { get; }
        public ICommand SendNotificationManuallyCommand { get; }

        public RepairRequestViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddRepairRequest());
            EditCommand = new RelayCommand(_ => EditRepairRequest(), canExecute => SelectedRepairRequest != null);
            DeleteCommand = new RelayCommand(_ => DeleteRepairRequest(), canExecute => SelectedRepairRequest != null);
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());
            ChangeStatusCommand = new RelayCommand(ChangeStatus, CanChangeStatus);
            SendNotificationManuallyCommand = new RelayCommand(SendNotificationManually, CanSendNotification);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var freshList = _model.GetRepairRequests().OrderByDescending(r => r.StartDate).ToList();

                using (var context = new ApplicationContext())
                {
                    foreach (var request in freshList)
                    {
                        if (request.ServiceId > 0)
                        {
                            request.Service = context.Services.FirstOrDefault(s => s.Id == request.ServiceId);
                            request.ServiceName = request.Service?.Name ?? "Не указана";
                        }

                        if (request.Status == null && request.StatusId > 0)
                        {
                            request.Status = context.StatusRequests.FirstOrDefault(s => s.Id == request.StatusId);
                        }
                    }
                }

                RepairRequests = new ObservableCollection<RepairRequest>(freshList);
                FilteredRequests = new ObservableCollection<RepairRequest>(freshList);

                var realStatuses = _model.GetStatuses();
                Statuses = new ObservableCollection<StatusRequest>(realStatuses);

                FilterStatuses = new ObservableCollection<StatusFilterItem>();
                FilterStatuses.Add(new StatusFilterItem { Id = null, Name = "Все заявки" });
                foreach (var status in realStatuses)
                {
                    FilterStatuses.Add(new StatusFilterItem { Id = status.Id, Name = status.Name });
                }

                SelectedFilterStatus = FilterStatuses.FirstOrDefault();

                OnPropertyChanged(nameof(RepairRequests));
                OnPropertyChanged(nameof(FilteredRequests));
                OnPropertyChanged(nameof(Statuses));
                OnPropertyChanged(nameof(FilterStatuses));
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                RepairRequests = new ObservableCollection<RepairRequest>();
                FilteredRequests = new ObservableCollection<RepairRequest>();
                Statuses = new ObservableCollection<StatusRequest>();
                FilterStatuses = new ObservableCollection<StatusFilterItem>();
                FilterStatuses.Add(new StatusFilterItem { Id = null, Name = "Все заявки" });
                SelectedFilterStatus = FilterStatuses.FirstOrDefault();

                OnPropertyChanged(nameof(RepairRequests));
                OnPropertyChanged(nameof(FilteredRequests));
                OnPropertyChanged(nameof(FilterStatuses));
            }
        }

        private void Filter()
        {
            if (RepairRequests == null) return;

            var query = RepairRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                query = query.Where(r =>
                    (r.Car?.RegistrationNumber?.ToLowerInvariant().Contains(search) == true) ||
                    (r.ClientDisplayName?.ToLowerInvariant().Contains(search) == true));
            }

            if (SelectedFilterStatus != null && SelectedFilterStatus.Id.HasValue)
            {
                query = query.Where(r => r.StatusId == SelectedFilterStatus.Id.Value);
            }

            FilteredRequests = new ObservableCollection<RepairRequest>(
                query.OrderByDescending(r => r.StartDate));

            OnPropertyChanged(nameof(FilteredRequests));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedFilterStatus = FilterStatuses?.FirstOrDefault();
        }

        private void OnRepairRequestSaved(object sender, EventArgs e)
        {
            LoadData();

            if (sender is AddRepairRequestViewModel viewModel)
            {
                viewModel.RepairRequestSaved -= OnRepairRequestSaved;
            }
        }

        private void AddRepairRequest()
        {
            try
            {
                var window = new AddRepairView();
                var viewModel = new AddRepairRequestViewModel();
                viewModel.RepairRequestSaved += OnRepairRequestSaved;
                window.DataContext = viewModel;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка при открытии окна добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRepairRequest()
        {
            try
            {
                var window = new AddRepairView();
                var viewModel = new AddRepairRequestViewModel(SelectedRepairRequest);
                viewModel.RepairRequestSaved += OnRepairRequestSaved;
                window.DataContext = viewModel;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRepairRequest()
        {
            var result = CustomMessageBox.Show($"Удалить заявку #{SelectedRepairRequest.Id}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _model.DeleteRepairRequest(SelectedRepairRequest);
                    LoadData();
                    CustomMessageBox.Show("Заявка успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task UpdateRepairStatus(RepairRequest request, int newStatusId)
        {
            if (request == null) return;

            int oldStatusId = request.StatusId;

            try
            {
                var updatedRequest = _model.UpdateRequestStatus(request.Id, newStatusId);

                if (updatedRequest != null)
                {
                    request.StatusId = newStatusId;

                    await _notificationService.SendNotificationOnStatusChange(updatedRequest, oldStatusId, newStatusId);
                }
                else
                {
                    throw new Exception("Не удалось обновить статус в базе данных.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось обновить статус заявки: {ex.Message}", ex);
            }
        }

        private bool CanChangeStatus(object parameter)
        {
            return SelectedRepairRequest != null;
        }

        private async void ChangeStatus(object parameter)
        {
            if (SelectedRepairRequest == null) return;

            var newStatusId = parameter as int?;

            if (!newStatusId.HasValue)
            {
                var statusDialog = new SelectStatusView(Statuses, SelectedRepairRequest.StatusId);
                if (statusDialog.ShowDialog() != true)
                    return;

                newStatusId = statusDialog.SelectedStatusId;
            }

            try
            {
                await UpdateRepairStatus(SelectedRepairRequest, newStatusId.Value);

                var statusName = Statuses.FirstOrDefault(s => s.Id == newStatusId.Value)?.Name ?? "новый";
                CustomMessageBox.Show($"Статус заявки изменен на '{statusName}'", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSendNotification(object parameter)
        {
            return SelectedRepairRequest != null &&
                   SelectedRepairRequest.StatusId == 3;
        }

        private async void SendNotificationManually(object parameter)
        {
            if (SelectedRepairRequest?.Car?.Client == null) return;

            var client = SelectedRepairRequest.Car.Client;
            var carInfo = $"{SelectedRepairRequest.Car.Brand} {SelectedRepairRequest.Car.Model} " +
                          $"({SelectedRepairRequest.Car.RegistrationNumber})";

            var dialog = new SendNotificationView(client.FullName, client.Email, client.ContactNumber, carInfo);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                bool emailSent = false;
                bool smsSent = false;

                if (dialog.SendEmail && !string.IsNullOrWhiteSpace(client.Email))
                {
                    var subject = $"Автомобиль готов к выдаче - заказ № {SelectedRepairRequest.Id}";

                    var htmlBody = GenerateBeautifulHtmlEmail(SelectedRepairRequest, client, carInfo);

                    emailSent = await new EmailService().SendEmailAsync(client.Email, subject, htmlBody);
                }

                if (dialog.SendSms && !string.IsNullOrWhiteSpace(client.ContactNumber))
                {
                    var smsService = new SmsService();
                    smsSent = await smsService.SendSmsAsync(client.ContactNumber,
                        $"Уважаемый(ая) {client.FullName}! Ваш автомобиль {carInfo} готов к выдаче. Ждем Вас в автосервисе.");
                }

                if (emailSent || smsSent)
                {
                    CustomMessageBox.Show("Уведомления успешно отправлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CustomMessageBox.Show("Не удалось отправить уведомления.\nПроверьте настройки почты и SMS.",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private string GenerateBeautifulHtmlEmail(RepairRequest request, Client client, string carInfo)
        {
            var workItems = DbManager.GetWorkItemsByRequestId(request.Id);

            decimal totalCost = 0;
            var itemsHtml = new System.Text.StringBuilder();
            int counter = 1;

            if (workItems != null && workItems.Any())
            {
                foreach (var wi in workItems)
                {
                    string serviceName = wi.Service?.Name ?? wi.Consumable?.Name ?? "Работа";
                    string employeeName = wi.Employee != null ? $"{wi.Employee.FirstName} {wi.Employee.LastName}" : "Не назначен";
                    decimal cost = wi.Cost;
                    totalCost += cost;

                    itemsHtml.AppendLine($@"
                <tr style='border-bottom: 1px solid #e0e0e0;'>
                    <td style='padding: 10px; text-align: center;'>{counter++}</td>
                    <td style='padding: 10px;'>{serviceName}</td>
                    <td style='padding: 10px; text-align: center;'>1</td>
                    <td style='padding: 10px; text-align: right;'>{cost:N2}</td>
                </tr>");
                }
            }
            else
            {
                totalCost = request.TotalCost;
                itemsHtml.AppendLine($@"
            <tr style='border-bottom: 1px solid #e0e0e0;'>
                <td style='padding: 10px; text-align: center;'>1</td>
                <td style='padding: 10px;'>Ремонт автомобиля {carInfo}</td>
                <td style='padding: 10px; text-align: center;'>1</td>
                <td style='padding: 10px; text-align: right;'>{totalCost:N2}</td>
            </td>");
            }

            var paymentStatusClass = "badge-not-paid";
            var paymentStatus = "Не оплачен";
            
            if (request.StatusId == 3 && totalCost > 0)
            {
                paymentStatusClass = "badge-paid";
                paymentStatus = "Оплачен";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Автомобиль готов к выдаче - Заказ № {request.Id}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1000px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #27AE60 0%, #1E8449 100%);
            color: white;
            padding: 25px 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .header p {{
            margin: 8px 0 0;
            opacity: 0.9;
            font-size: 14px;
        }}
        .content {{
            padding: 30px;
        }}
        .greeting {{
            margin-bottom: 25px;
            color: #2c3e50;
            line-height: 1.6;
        }}
        .greeting strong {{
            font-size: 16px;
        }}
        .info-block {{
            margin-bottom: 30px;
        }}
        .info-title {{
            font-weight: bold;
            font-size: 16px;
            color: #27AE60;
            border-left: 4px solid #27AE60;
            padding-left: 12px;
            margin-bottom: 15px;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            background: #f8f9fa;
            border-radius: 6px;
            overflow: hidden;
            font-size: 14px;
        }}
        .info-table td {{
            padding: 10px 15px;
            border-bottom: 1px solid #e0e0e0;
        }}
        .info-table td:first-child {{
            font-weight: bold;
            width: 35%;
            background: #f0f2f5;
        }}
        .info-table tr:last-child td {{
            border-bottom: none;
        }}
        .badge-paid {{
            color: #27ae60;
            font-weight: bold;
        }}
        .badge-not-paid {{
            color: #e74c3c;
            font-weight: bold;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
            font-size: 13px;
        }}
        .items-table th {{
            background: #27AE60;
            color: white;
            padding: 12px 8px;
            text-align: center;
            font-weight: 500;
            font-size: 13px;
        }}
        .items-table td {{
            padding: 10px 8px;
            border-bottom: 1px solid #e0e0e0;
        }}
        .items-table tr:hover {{
            background-color: #f5f5f5;
        }}
        .totals {{
            text-align: right;
            margin-top: 20px;
            padding-top: 15px;
            border-top: 2px solid #e0e0e0;
        }}
        .totals p {{
            margin: 5px 0;
            font-size: 14px;
        }}
        .totals .grand-total {{
            font-size: 18px;
            font-weight: bold;
            color: #27AE60;
            margin-top: 10px;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #7f8c8d;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        @media (max-width: 768px) {{
            .content {{
                padding: 15px;
            }}
            .items-table {{
                font-size: 11px;
            }}
            .items-table th,
            .items-table td {{
                padding: 6px 4px;
            }}
            .info-table td {{
                padding: 8px 10px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Ваш автомобиль готов к выдаче!</h1>
            <p>Заказ-наряд № {request.Id} выполнен</p>
        </div>
        
        <div class='content'>
            <div class='greeting'>
                <strong>Уважаемый(ая) {client.FullName}!</strong><br/><br/>
                Сообщаем Вам, что ремонт Вашего автомобиля успешно завершен.
            </div>

            <div class='info-block'>
                <div class='info-title'>Информация о заказе</div>
                <table class='info-table'>
                    <tr>
                        <td>Номер заказ-наряда</td>
                        <td><strong>№ {request.Id}</strong></td>
                    </tr>
                    <tr>
                        <td>Дата оформления</td>
                        <td>{request.StartDate:dd.MM.yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td>Автомобиль</td>
                        <td><strong>{carInfo}</strong></td>
                    </tr>
                    <tr>
                        <td>Клиент</td>
                        <td>{client.FullName}</td>
                    </tr>
                    <tr>
                        <td>Контактный телефон</td>
                        <td>{client.ContactNumber}</td>
                    </tr>
                    <tr>
                        <td>Email</td>
                        <td>{client.Email}</td>
                    </tr>
                    <tr>
                        <td>Статус оплаты</td>
                        <td class='{paymentStatusClass}'>{paymentStatus}</td>
                    </tr>
                </table>
            </div>

            <div class='info-block'>
                <div class='info-title'>Выполненные работы</div>
                <table class='items-table'>
                    <thead>
                        <tr>
                            <th>№</th>
                            <th>Наименование</th>
                            <th>Кол-во</th>
                            <th>Сумма</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsHtml}
                    </tbody>
                </table>
            </div>

            <div class='totals'>
                <p class='grand-total'><strong>Итого к оплате:</strong> {totalCost:N2} ₽</p>
            </div>

            <div class='greeting'>
                🚗 <strong>Ждем Вас в нашем автосервисе для получения автомобиля!</strong><br/><br/>
                <strong>Режим работы:</strong> Пн-Пт: 9:00 - 20:00, Сб-Вс: 10:00 - 18:00<br/>
                📞 <strong>Контакты:</strong> +7 (952) 724-14-21
            </div>
        </div>
        
        <div class='footer'>
            <p>Это письмо сформировано автоматически. Пожалуйста, не отвечайте на него.</p>
            <p>© {DateTime.Now.Year} Автосервис. Все права защищены.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
    public class StatusFilterItem
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }
}
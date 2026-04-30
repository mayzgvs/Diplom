using Service.Data;
using Service.Models;
using Service.Services;
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

        // Специальная коллекция для фильтрации с дополнительным элементом "Все заявки"
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

        // Изменяем тип свойства на StatusFilterItem
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
                var freshList = _model.GetRepairRequests();

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

                // Получаем реальные статусы из БД
                var realStatuses = _model.GetStatuses();
                Statuses = new ObservableCollection<StatusRequest>(realStatuses);

                // Создаем коллекцию для фильтрации с элементом "Все заявки"
                FilterStatuses = new ObservableCollection<StatusFilterItem>();
                FilterStatuses.Add(new StatusFilterItem { Id = null, Name = "Все заявки" });
                foreach (var status in realStatuses)
                {
                    FilterStatuses.Add(new StatusFilterItem { Id = status.Id, Name = status.Name });
                }

                // Устанавливаем фильтр по умолчанию - "Все заявки"
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

            // Фильтрация по статусу с учетом "Все заявки" (когда Id == null)
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

            // Используем новое окно подтверждения (лучше, чем было)
            var dialog = new SendNotificationView(client.FullName, client.Email, client.ContactNumber, carInfo);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                bool emailSent = false;
                bool smsSent = false;

                if (dialog.SendEmail && !string.IsNullOrWhiteSpace(client.Email))
                {
                    var subject = "Автомобиль готов к выдаче";
                    var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #3498DB;'>Уважаемый(ая) {client.FullName}!</h2>
                    <p>Ваш автомобиль <strong>{carInfo}</strong> готов к выдаче.</p>
                    <p>Ждем Вас в нашем автосервисе.</p>
                    <br/>
                    <hr/>
                    <p style='color: #7F8C8D; font-size: 12px;'>Это сообщение отправлено автоматически.</p>
                </body>
                </html>";

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
    }
    public class StatusFilterItem
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }
}
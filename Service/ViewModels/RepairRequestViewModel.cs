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

        private StatusRequest _selectedFilterStatus;
        public StatusRequest SelectedFilterStatus
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
            var freshList = _model.GetRepairRequests();

            RepairRequests = new ObservableCollection<RepairRequest>(freshList);
            FilteredRequests = new ObservableCollection<RepairRequest>(freshList);

            Statuses = new ObservableCollection<StatusRequest>(_model.GetStatuses());

            OnPropertyChanged(nameof(RepairRequests));
            OnPropertyChanged(nameof(FilteredRequests));
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

            if (SelectedFilterStatus != null)
            {
                query = query.Where(r => r.StatusId == SelectedFilterStatus.Id);
            }

            FilteredRequests = new ObservableCollection<RepairRequest>(
                query.OrderByDescending(r => r.StartDate));

            OnPropertyChanged(nameof(FilteredRequests));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedFilterStatus = null;
        }

        private void AddRepairRequest()
        {
            var window = new AddRepairView();
            window.DataContext = new AddRepairRequestViewModel();

            if (window.ShowDialog() == true)
                LoadData();
        }

        private void EditRepairRequest()
        {
            var window = new AddRepairView();
            window.DataContext = new AddRepairRequestViewModel(SelectedRepairRequest);

            if (window.ShowDialog() == true)
                LoadData();
        }

        private void DeleteRepairRequest()
        {
            if (MessageBox.Show($"Удалить заявку #{SelectedRepairRequest.Id}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _model.DeleteRepairRequest(SelectedRepairRequest);
                LoadData();
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
                MessageBox.Show($"Статус заявки изменен на '{statusName}'", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка",
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
                if (dialog.SendEmail && !string.IsNullOrEmpty(client.Email))
                {
                    var emailService = new EmailService();
                    await emailService.SendEmailAsync(client.Email, "Автомобиль готов к выдаче",
                        $"Уважаемый(ая) {client.FullName}! Ваш автомобиль {carInfo} готов к выдаче.");
                }

                if (dialog.SendSms && !string.IsNullOrEmpty(client.ContactNumber))
                {
                    var smsService = new SmsService();
                    await smsService.SendSmsAsync(client.ContactNumber,
                        $"Уважаемый(ая) {client.FullName}! Автомобиль {carInfo} готов к выдаче.");
                }

                MessageBox.Show("Уведомления отправлены", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
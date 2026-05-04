using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class ClientViewModel : BaseViewModel
    {
        private readonly ClientModel _model = new ClientModel();

        public ObservableCollection<Client> Clients { get; private set; }
        public ObservableCollection<Client> FilteredClients { get; private set; }

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterClients();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public ClientViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddClient());
            EditCommand = new RelayCommand(_ => EditClient(), _ => SelectedClient != null);
            DeleteCommand = new RelayCommand(_ => DeleteClient(), _ => SelectedClient != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = "");

            Clients = new ObservableCollection<Client>();
            FilteredClients = new ObservableCollection<Client>();
            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetClients().OrderBy(c => c.LastName).ThenBy(c => c.FirstName);

            Clients.Clear();
            FilteredClients.Clear();

            foreach (var client in list)
            {
                Clients.Add(client);
                FilteredClients.Add(client);
            }

            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(FilteredClients));
        }

        private void FilterClients()
        {
            if (Clients == null) return;

            var search = (SearchText ?? "").Trim().ToLower();

            FilteredClients.Clear();

            if (string.IsNullOrEmpty(search))
            {
                foreach (var client in Clients)
                {
                    FilteredClients.Add(client);
                }
            }
            else
            {
                var filtered = Clients.Where(c =>
                    (c.LastName?.ToLower().Contains(search) == true) ||
                    (c.FirstName?.ToLower().Contains(search) == true) ||
                    (c.ContactNumber?.ToLower().Contains(search) == true) ||
                    (c.Email?.ToLower().Contains(search) == true)
                );

                foreach (var client in filtered)
                {
                    FilteredClients.Add(client);
                }
            }
        }

        private void AddClient()
        {
            var window = new AddClient();
            var viewModel = new AddClientViewModel();

            viewModel.ClientSaved += OnClientSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void EditClient()
        {
            var window = new AddClient();
            var viewModel = new AddClientViewModel(SelectedClient);

            viewModel.ClientSaved += OnClientSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void OnClientSaved(object sender, EventArgs e)
        {
            LoadData();
            FilterClients();

            if (sender is AddClientViewModel viewModel)
            {
                viewModel.ClientSaved -= OnClientSaved;
            }
        }

        private void DeleteClient()
        {
            if (SelectedClient == null) return;

            // Сохраняем информацию о клиенте до удаления
            var clientToDelete = SelectedClient;
            var clientName = clientToDelete.FullName;

            // Получаем информацию о связанных данных до удаления
            var carsCount = _model.GetCarsByClientId(clientToDelete.Id).Count;
            var requestsCount = _model.GetRepairRequestsCountByClientId(clientToDelete.Id);

            string warningMessage;
            MessageBoxImage icon = MessageBoxImage.Question;

            if (carsCount > 0 || requestsCount > 0)
            {
                warningMessage = $"Вы действительно хотите удалить клиента {clientName}?\n\n" +
                                $"ВНИМАНИЕ! Это приведет к КАСКАДНОМУ удалению:\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                $"• {carsCount} автомобилей\n" +
                                $"• {requestsCount} заявок на ремонт\n" +
                                $"• Всех связанных работ и материалов\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                $"Данное действие НЕОБРАТИМО!\n\n" +
                                $"Вы уверены, что хотите продолжить?";
                icon = MessageBoxImage.Warning;
            }
            else
            {
                warningMessage = $"Удалить клиента {clientName}?";
            }

            var result = CustomMessageBox.Show(warningMessage,
                "Подтверждение каскадного удаления", MessageBoxButton.YesNo, icon);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Выполняем удаление
                    _model.DeleteClient(clientToDelete);

                    // Очищаем выбранный элемент
                    SelectedClient = null;

                    // Обновляем списки
                    LoadData();
                    FilterClients();

                    // Показываем сообщение об успехе
                    var message = carsCount > 0 || requestsCount > 0
                        ? $"✓ Клиент и все связанные данные успешно удалены!\n\n" +
                          $"Удалено:\n" +
                          $"• Клиент: {clientName}\n" +
                          $"• Автомобилей: {carsCount}\n" +
                          $"• Заявок: {requestsCount}"
                        : $"✓ Клиент {clientName} успешно удален!";

                    CustomMessageBox.Show(message, "Каскадное удаление выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при каскадном удалении: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
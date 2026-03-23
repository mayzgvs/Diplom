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
            set { _selectedClient = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterClients(); }
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
            var list = _model.GetClients();

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
                    (c.ContactNumber?.ToLower().Contains(search) == true)
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

            if (MessageBox.Show($"Удалить клиента {SelectedClient.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _model.DeleteClient(SelectedClient);
                    LoadData();
                    FilterClients();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
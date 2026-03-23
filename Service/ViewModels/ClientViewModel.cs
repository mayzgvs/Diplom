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

        public ClientViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddClient());
            EditCommand = new RelayCommand(_ => EditClient(), _ => SelectedClient != null);
            DeleteCommand = new RelayCommand(_ => DeleteClient(), _ => SelectedClient != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetClients();
            Clients = new ObservableCollection<Client>(list);
            FilteredClients = new ObservableCollection<Client>(list);
        }

        private void FilterClients()
        {
            if (Clients == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Clients.ToList()
                : Clients.Where(c =>
                    (c.LastName?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (c.FirstName?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (c.ContactNumber?.ToLower().Contains(SearchText.ToLower()) == true)).ToList();

            FilteredClients = new ObservableCollection<Client>(filtered);
        }

        private void AddClient()
        {
            var window = new AddClient();
            window.DataContext = new AddClientViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditClient()
        {
            var window = new AddClient();
            window.DataContext = new AddClientViewModel(SelectedClient);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteClient()
        {
            if (MessageBox.Show($"Удалить клиента {SelectedClient.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteClient(SelectedClient);
                LoadData();
            }
        }
    }
}
using Service.Data;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class ClientViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        private ObservableCollection<Client> _clients;
        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Client> _filteredClients;
        public ObservableCollection<Client> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                OnPropertyChanged();
            }
        }

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged();
                if (value != null)
                {
                    EditingClient = new Client
                    {
                        Id = value.Id,
                        FirstName = value.FirstName,
                        LastName = value.LastName,
                        Discount = value.Discount,
                        ContactNumber = value.ContactNumber
                    };
                }
                else
                {
                    EditingClient = null;
                }
            }
        }

        private Client _editingClient;
        public Client EditingClient
        {
            get => _editingClient;
            set
            {
                _editingClient = value;
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public ClientViewModel()
        {
            _context = new ApplicationContext();
            Clients = new ObservableCollection<Client>();
            FilteredClients = new ObservableCollection<Client>();

            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
            AddCommand = new RelayCommand(AddNewClient);
            EditCommand = new RelayCommand(EditClient, CanEditOrDelete);
            SaveCommand = new RelayCommand(async (obj) => await SaveClientAsync(), CanSaveClient);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new RelayCommand(async (obj) => await DeleteClientAsync(), CanEditOrDelete);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var clients = await _context.Clients
                    .Include(c => c.Cars)
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();

                Clients = new ObservableCollection<Client>(clients);
                FilteredClients = new ObservableCollection<Client>(clients);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterClients()
        {
            if (Clients == null) return;

            var filtered = Clients.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    (c.LastName?.ToLower().Contains(searchLower) == true) ||
                    (c.FirstName?.ToLower().Contains(searchLower) == true) ||
                    (c.ContactNumber?.ToLower().Contains(searchLower) == true));
            }

            FilteredClients = new ObservableCollection<Client>(filtered);
        }

        private void ClearFilters(object obj)
        {
            SearchText = string.Empty;
        }

        private void AddNewClient(object obj)
        {
            var addWindow = new Views.AddClient();
            var viewModel = new AddClientViewModel(_context);
            addWindow.DataContext = viewModel;

            if (addWindow.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditClient(object obj)
        {
            if (SelectedClient != null)
            {
                var editWindow = new Views.AddClient();
                var viewModel = new AddClientViewModel(_context, SelectedClient);
                editWindow.DataContext = viewModel;

                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private async Task SaveClientAsync()
        {
            if (EditingClient == null) return;

            if (string.IsNullOrWhiteSpace(EditingClient.LastName))
            {
                MessageBox.Show("Фамилия обязательна для заполнения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingClient.FirstName))
            {
                MessageBox.Show("Имя обязательно для заполнения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (EditingClient.Id == 0) // Новый клиент
                {
                    _context.Clients.Add(EditingClient);
                    await _context.SaveChangesAsync();

                    Clients.Add(EditingClient);
                    FilteredClients.Add(EditingClient);
                }
                else // Редактирование существующего
                {
                    var clientToUpdate = await _context.Clients.FindAsync(EditingClient.Id);
                    if (clientToUpdate != null)
                    {
                        clientToUpdate.FirstName = EditingClient.FirstName;
                        clientToUpdate.LastName = EditingClient.LastName;
                        clientToUpdate.Discount = EditingClient.Discount;
                        clientToUpdate.ContactNumber = EditingClient.ContactNumber;

                        await _context.SaveChangesAsync();

                        var existingClient = Clients.FirstOrDefault(c => c.Id == EditingClient.Id);
                        if (existingClient != null)
                        {
                            existingClient.FirstName = EditingClient.FirstName;
                            existingClient.LastName = EditingClient.LastName;
                            existingClient.Discount = EditingClient.Discount;
                            existingClient.ContactNumber = EditingClient.ContactNumber;
                        }

                        FilterClients();
                    }
                }

                EditingClient = null;
                SelectedClient = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit(object obj)
        {
            EditingClient = null;
            SelectedClient = null;
        }

        private async Task DeleteClientAsync()
        {
            if (SelectedClient == null) return;

            // Проверяем наличие автомобилей у клиента
            var hasCars = await _context.Cars.AnyAsync(c => c.OwnerId == SelectedClient.Id);
            if (hasCars)
            {
                MessageBox.Show("Невозможно удалить клиента, у которого есть автомобили. " +
                    "Сначала удалите все автомобили клиента.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента {SelectedClient.LastName} {SelectedClient.FirstName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var clientToDelete = await _context.Clients.FindAsync(SelectedClient.Id);
                if (clientToDelete != null)
                {
                    _context.Clients.Remove(clientToDelete);
                    await _context.SaveChangesAsync();

                    Clients.Remove(SelectedClient);
                    FilteredClients.Remove(SelectedClient);
                    SelectedClient = null;
                    EditingClient = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanEditOrDelete(object obj)
        {
            return SelectedClient != null;
        }

        private bool CanSaveClient(object obj)
        {
            return EditingClient != null && !IsLoading;
        }
    }
}
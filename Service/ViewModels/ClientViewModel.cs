using Service.Data;
using Service.ViewModels;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class ClientViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;

        // Коллекция для отображения в DataGrid
        private ObservableCollection<Client> _clients;
        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged(nameof(Clients));
                FilteredClients = new ObservableCollection<Client>(value); // Обновляем отфильтрованную коллекцию
            }
        }

        // Отфильтрованная коллекция для отображения в DataGrid
        private ObservableCollection<Client> _filteredClients;
        public ObservableCollection<Client> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                OnPropertyChanged(nameof(FilteredClients));
            }
        }

        private Client _currentClient;
        public Client CurrentClient
        {
            get => _currentClient;
            set
            {
                _currentClient = value;
                OnPropertyChanged(nameof(CurrentClient));

                SelectedClient = value;
            }
        }

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));

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

                    if (_currentClient != value)
                        _currentClient = value;
                }
                else
                {
                    EditingClient = null;
                    _currentClient = null;
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
                OnPropertyChanged(nameof(EditingClient));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterClients();
            }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
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
            FilteredClients = new ObservableCollection<Client>(Clients);
        }

        private void AddNewClient(object obj)
        {
            var editWindow = new AddClient();
            editWindow.DataContext = this; // Передаем текущую ViewModel
            EditingClient = new Client { Discount = 0 };
            editWindow.ShowDialog(); // Открываем как диалог
        }

        private void EditClient(object obj)
        {
            if (SelectedClient != null)
            {
                var editWindow = new AddClient();
                editWindow.DataContext = this;
                editWindow.Title = "Редактирование клиента";
                editWindow.ShowDialog();
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
                if (EditingClient.Id == 0) 
                {
                    _context.Clients.Add(EditingClient);
                    await _context.SaveChangesAsync();

                    Clients.Add(EditingClient);
                    FilteredClients.Add(EditingClient);
                }
                else 
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
                CurrentClient = null;
                IsEditMode = false;
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
            CurrentClient = null;
            IsEditMode = false;
        }

        private async Task DeleteClientAsync()
        {
            if (SelectedClient == null) return;

            if (SelectedClient.Cars != null && SelectedClient.Cars.Any())
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
                    CurrentClient = null;
                    EditingClient = null;
                    IsEditMode = false;
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
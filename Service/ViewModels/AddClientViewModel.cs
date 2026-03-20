using Service.Data;
using System;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddClientViewModel : BaseViewModel
    {
        private readonly ApplicationContext _context;
        private Client _editingClient;
        private bool _isEditMode;

        public Client EditingClient
        {
            get => _editingClient;
            set
            {
                _editingClient = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelEditCommand { get; set; }

        public AddClientViewModel(ApplicationContext context, Client client = null)
        {
            _context = context;

            if (client == null)
            {
                _isEditMode = false;
                EditingClient = new Client { Discount = 0 };
            }
            else
            {
                _isEditMode = true;
                EditingClient = new Client
                {
                    Id = client.Id,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Discount = client.Discount,
                    ContactNumber = client.ContactNumber
                };
            }

            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void Save(object parameter)
        {
            try
            {
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

                if (!_isEditMode)
                {
                    _context.Clients.Add(EditingClient);
                }
                else
                {
                    var existing = _context.Clients.Find(EditingClient.Id);
                    if (existing != null)
                    {
                        existing.FirstName = EditingClient.FirstName;
                        existing.LastName = EditingClient.LastName;
                        existing.ContactNumber = EditingClient.ContactNumber;
                        existing.Discount = EditingClient.Discount;
                    }
                }

                _context.SaveChanges();

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
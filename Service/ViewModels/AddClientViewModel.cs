using Service.Data;
using Service.Models;
using Service.Utility;
using System;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class AddClientViewModel : BaseViewModel
    {
        private readonly ClientAddEditModel _model = new ClientAddEditModel();
        private Client _editingClient;
        private bool _isEditMode;

        public event EventHandler ClientSaved;

        public Client EditingClient
        {
            get => _editingClient;
            set 
            { 
                _editingClient = value; 
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public AddClientViewModel(Client client = null)
        {
            if (client == null)
            {
                _isEditMode = false;
                EditingClient = new Client();
            }
            else
            {
                _isEditMode = true;
                EditingClient = client;
            }

            SaveCommand = new RelayCommand(Save);   
            CancelEditCommand = new RelayCommand(Cancel);
        }

        private void Save(object parameter)
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(EditingClient.LastName) ||
                string.IsNullOrWhiteSpace(EditingClient.FirstName))
            {
                ErrorMessage = "Введите фамилию и имя клиента!";
                MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingClient.ContactNumber))
                {
                    ErrorMessage = "Некорректный формат номера телефона!\nПример: +7XXXXXXXXXX";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.PhoneExists(EditingClient.ContactNumber, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким номером телефона уже существует!";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.Email))
            {
                if (!ValidationHelper.IsValidEmail(EditingClient.Email))
                {
                    ErrorMessage = "Некорректный формат email-адреса!";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.EmailExists(EditingClient.Email, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким email уже существует!";
                    MessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateClient(EditingClient.FirstName, EditingClient.LastName,
                                      EditingClient.ContactNumber, EditingClient.Email);
                    MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditClient(EditingClient.Id, EditingClient.FirstName, EditingClient.LastName,
                                    EditingClient.ContactNumber, EditingClient.Email);
                    MessageBox.Show("Клиент успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClientSaved?.Invoke(this, EventArgs.Empty);

                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
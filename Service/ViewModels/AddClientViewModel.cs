using Service.Data;
using Service.Models;
using Service.Utility;
using Service.Views;
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
                CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingClient.ContactNumber))
                {
                    ErrorMessage = "Некорректный формат номера телефона!\nПример: +7XXXXXXXXXX";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.PhoneExists(EditingClient.ContactNumber, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким номером телефона уже существует!";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.Email))
            {
                if (!ValidationHelper.IsValidEmail(EditingClient.Email))
                {
                    ErrorMessage = "Некорректный формат email-адреса!";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_model.EmailExists(EditingClient.Email, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким email уже существует!";
                    CustomMessageBox.Show(ErrorMessage, "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (!_isEditMode)
                {
                    _model.CreateClient(EditingClient.FirstName, EditingClient.LastName,
                                      EditingClient.ContactNumber, EditingClient.Email);
                    CustomMessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _model.EditClient(EditingClient.Id, EditingClient.FirstName, EditingClient.LastName,
                                    EditingClient.ContactNumber, EditingClient.Email);
                    CustomMessageBox.Show("Клиент успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClientSaved?.Invoke(this, EventArgs.Empty);

                // Закрываем окно после MessageBox
                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                CustomMessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
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
            set { _editingClient = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        private async void Save(object parameter)
        {
            ErrorMessage = "";
            SuccessMessage = "";

            if (string.IsNullOrWhiteSpace(EditingClient.LastName) || string.IsNullOrWhiteSpace(EditingClient.FirstName))
            {
                ErrorMessage = "Заполните обязательные поля!";
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.ContactNumber))
            {
                if (!ValidationHelper.IsValidRussianPhone(EditingClient.ContactNumber))
                {
                    ErrorMessage = "Ошибка ввода: неверный формат телефона!";
                    return;
                }

                if (_model.PhoneExists(EditingClient.ContactNumber, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким номером телефона уже существует!";
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EditingClient.Email))
            {
                if (!ValidationHelper.IsValidEmail(EditingClient.Email))
                {
                    ErrorMessage = "Некорректный формат email!";
                    return;
                }

                if (_model.EmailExists(EditingClient.Email, _isEditMode ? EditingClient.Id : (int?)null))
                {
                    ErrorMessage = "Клиент с таким email уже существует!";
                    return;
                }
            }

            try
            {
                if (!_isEditMode)
                    _model.CreateClient(EditingClient.FirstName, EditingClient.LastName,
                        EditingClient.ContactNumber, EditingClient.Email);
                else
                    _model.EditClient(EditingClient.Id, EditingClient.FirstName, EditingClient.LastName,
                        EditingClient.ContactNumber, EditingClient.Email);

                SuccessMessage = _isEditMode ? "Клиент успешно обновлен!" : "Клиент успешно добавлен!";
                ClientSaved?.Invoke(this, EventArgs.Empty);

                await System.Threading.Tasks.Task.Delay(800);

                if (parameter is Window window)
                    window.DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
            }
        }

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

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
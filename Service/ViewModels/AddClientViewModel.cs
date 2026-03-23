using Service.Data;
using Service.Models;
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

        public Client EditingClient
        {
            get => _editingClient;
            set { _editingClient = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public AddClientViewModel(Client client = null)
        {
            if (client == null)
            {
                _isEditMode = false;
                EditingClient = new Client { Discount = 0 };
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
            if (string.IsNullOrWhiteSpace(EditingClient.LastName) || string.IsNullOrWhiteSpace(EditingClient.FirstName))
            {
                MessageBox.Show("Фамилия и имя обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isEditMode)
                _model.CreateClient(EditingClient.FirstName, EditingClient.LastName, EditingClient.ContactNumber, EditingClient.Discount);
            else
                _model.EditClient(EditingClient.Id, EditingClient.FirstName, EditingClient.LastName, EditingClient.ContactNumber, EditingClient.Discount);

            if (parameter is Window window)
                window.DialogResult = true;
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
                window.DialogResult = false;
        }
    }
}
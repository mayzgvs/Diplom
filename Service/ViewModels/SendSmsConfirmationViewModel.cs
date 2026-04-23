using System.Windows.Input;

namespace Service.ViewModels
{
    public class SendSmsConfirmationViewModel : BaseViewModel
    {
        private string _clientPhone;
        private string _clientName;
        private string _carInfo;
        private bool _sendSms = true;

        public string ClientPhone
        {
            get => _clientPhone;
            set 
            { 
                _clientPhone = value; 
                OnPropertyChanged(); 
            }
        }

        public string ClientName
        {
            get => _clientName;
            set 
            { 
                _clientName = value; 
                OnPropertyChanged();
            }
        }

        public string CarInfo
        {
            get => $"{_carInfo}";
            set 
            { 
                _carInfo = value; 
                OnPropertyChanged();
            }
        }

        public bool SendSms
        {
            get => _sendSms;
            set 
            { 
                _sendSms = value; 
                OnPropertyChanged(); 
            }
        }

        public string MessagePreview
        {
            get
            {
                return $"Уважаемый(ая) {ClientName}! Ваш автомобиль {CarInfo} готов к выдаче. " +
                       $"Ждем Вас в автосервисе.";
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public SendSmsConfirmationViewModel(string clientName, string clientPhone, string carInfo)
        {
            ClientName = clientName;
            ClientPhone = clientPhone;
            CarInfo = carInfo;

            ConfirmCommand = new RelayCommand(_ => Confirm());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public bool? DialogResult { get; private set; }

        private void Confirm()
        {
            DialogResult = SendSms;
        }

        private void Cancel()
        {
            DialogResult = null;
        }
    }
}
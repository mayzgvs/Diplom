using System.Windows;

namespace Service.Views
{
    public partial class SendNotificationView : Window
    {
        public bool SendEmail { get; private set; }
        public bool SendSms { get; private set; }

        public SendNotificationView(string clientName, string email, string phone, string carInfo)
        {
            InitializeComponent();

            ClientNameText.Text = $"Клиент: {clientName}";
            ClientEmailText.Text = $"Email: {email ?? "—"}";
            ClientPhoneText.Text = $"Телефон: {phone ?? "—"}";
            CarInfoText.Text = $"Автомобиль: {carInfo}";

            SendEmailCheckBox.IsChecked = true;
            SendSmsCheckBox.IsChecked = true;

            MessageTextBox.Text = $"Уважаемый(ая) {clientName}! Ваш автомобиль {carInfo} готов к выдаче. Ждем Вас в автосервисе.";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendEmail = SendEmailCheckBox.IsChecked == true;
            SendSms = SendSmsCheckBox.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
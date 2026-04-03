using Service.Data;
using Service.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Service.Views
{
    public partial class SelectStatusView : Window
    {
        public int? SelectedStatusId { get; private set; }

        public SelectStatusView(ObservableCollection<StatusRequest> statuses, int currentStatusId)
        {
            InitializeComponent();

            StatusComboBox.ItemsSource = statuses;

            var currentStatus = statuses.FirstOrDefault(s => s.Id == currentStatusId);
            if (currentStatus != null)
            {
                StatusComboBox.SelectedItem = currentStatus;
            }
            else if (statuses.Any())
            {
                StatusComboBox.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (StatusComboBox.SelectedItem is StatusRequest selected)
            {
                SelectedStatusId = selected.Id;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите статус.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
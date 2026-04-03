using System.Windows;
using System.Windows.Input;

namespace Service.Views
{
    public partial class AddWorkItemView : Window
    {
        public AddWorkItemView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
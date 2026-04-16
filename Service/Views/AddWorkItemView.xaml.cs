using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Service.Views
{
    public partial class AddWorkItemView : Window
    {
        public AddWorkItemView()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
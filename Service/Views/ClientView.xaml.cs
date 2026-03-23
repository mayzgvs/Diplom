using System;
using System.Windows;
using System.Windows.Controls;
using Service.ViewModels;

namespace Service.Views
{
    public partial class ClientView : UserControl
    {
        private readonly ClientViewModel _viewModel;

        public ClientView()
        {
            InitializeComponent();

            _viewModel = new ClientViewModel();
            DataContext = _viewModel;

            // Загрузка данных при появлении контрола
            Loaded += ClientView_Loaded;
        }

        private void ClientView_Loaded(object sender, RoutedEventArgs e)
        {
            // Запускаем асинхронную загрузку через команду
            _viewModel.LoadedCommand.Execute(null);
        }
    }
}
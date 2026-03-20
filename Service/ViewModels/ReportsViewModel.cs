using Service.ViewModels;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; }

        public ReportsViewModel()
        {
            LoadedCommand = new RelayCommand(async (obj) => await LoadDataAsync());
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                // Здесь будет загрузка данных для отчетов
                // Например:
                // await LoadStatisticsAsync();
                // await LoadRevenueReportAsync();
                // и т.д.

                await System.Threading.Tasks.Task.Delay(100); // Имитация загрузки
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
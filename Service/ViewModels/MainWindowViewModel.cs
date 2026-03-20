using Service.Views;
using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace Service.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        // Свойство для доступа к навигации
        public NavigationViewModel Navigation { get; }

        // Текущая дата и время
        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        private string _currentTime;
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        // Команда для навигации (прокси к навигации)
        public ICommand NavigateCommand => Navigation?.NavigateToCommand;

        private readonly DispatcherTimer _timer;

        public MainWindowViewModel()
        {
            // Инициализируем навигацию
            Navigation = new NavigationViewModel();

            Debug.WriteLine("MainWindowViewModel created");

            // Таймер для даты/времени
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentDate = now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
            CurrentTime = now.ToString("HH:mm");
        }
    }
}
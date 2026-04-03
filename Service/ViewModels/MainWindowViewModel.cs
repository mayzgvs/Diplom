using Service.Views;
using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace Service.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public NavigationViewModel Navigation { get; }

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

        public ICommand NavigateCommand => Navigation?.NavigateToCommand;

        private readonly DispatcherTimer _timer;

        public MainWindowViewModel()
        {
            Navigation = new NavigationViewModel();

            Debug.WriteLine("MainWindowViewModel created");

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
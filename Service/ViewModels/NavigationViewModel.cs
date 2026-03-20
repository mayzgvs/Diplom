using Service.ViewModels;
using System.Diagnostics;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class NavigationViewModel : BaseViewModel
    {
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
                Debug.WriteLine($"CurrentViewModel changed to: {value?.GetType().Name ?? "null"}");
            }
        }

        public ICommand NavigateToCommand { get; }

        public NavigationViewModel()
        {
            NavigateToCommand = new RelayCommand(NavigateTo);
            Debug.WriteLine("NavigationViewModel created, NavigateToCommand initialized");
        }

        private void NavigateTo(object parameter)
        {
            Debug.WriteLine($"NavigateTo вызван с параметром: {parameter}");

            if (parameter is string viewModelName)
            {
                Debug.WriteLine($"Создаем ViewModel для: {viewModelName}");

                switch (viewModelName)
                {
                    case "Clients":
                        CurrentViewModel = new ClientViewModel();
                        break;
                    case "Cars":
                        CurrentViewModel = new CarViewModel();
                        break;
                    case "RepairRequests":
                        CurrentViewModel = new RepairRequestViewModel();
                        break;
                    case "Services":
                        CurrentViewModel = new ServiceViewModel();
                        break;
                    case "Consumables":
                        CurrentViewModel = new ConsumableViewModel();
                        break;
                    case "Employees":
                        CurrentViewModel = new EmployeeViewModel();
                        break;
                    case "Reports":
                        CurrentViewModel = new ReportsViewModel();
                        break;
                    default:
                        CurrentViewModel = null;
                        break;
                }

                Debug.WriteLine($"После навигации CurrentViewModel: {CurrentViewModel?.GetType().Name ?? "null"}");
            }
        }
    }
}
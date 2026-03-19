using Service;
using Service.ViewModels;
using System.Windows.Input;

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
        }
    }

    public ICommand NavigateToCommand { get; }

    public NavigationViewModel()
    {
        NavigateToCommand = new RelayCommand(NavigateTo);
        // Стартуем с приветствием
        CurrentViewModel = null;
    }

    private void NavigateTo(object parameter)
    {
        if (parameter is string viewModelName)
        {
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
        }
    }
}
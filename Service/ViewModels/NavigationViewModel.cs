using Service.ViewModels; // Убедитесь, что это правильное пространство имен для ваших ViewModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class NavigationViewModel : BaseViewModel
    {
        // Текущая ViewModel, которая отображается в главном окне
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

        // Команды для навигации. В качестве параметра можно передавать Type (тип ViewModel)
        public ICommand NavigateToCommand { get; }

        public NavigationViewModel()
        {
            // Инициализация команды навигации
            NavigateToCommand = new RelayCommand(NavigateTo);

            // Устанавливаем стартовую страницу (например, Клиенты)
            CurrentViewModel = new ClientViewModel(); // или любая другая ViewModel по умолчанию
        }

        // Метод для навигации
        private void NavigateTo(object parameter)
        {
            // Ожидаем, что параметр — это тип ViewModel (например, typeof(ClientViewModel))
            if (parameter is Type viewModelType && typeof(BaseViewModel).IsAssignableFrom(viewModelType))
            {
                // Создаем экземпляр ViewModel через Activator
                // Можно также использовать DI-контейнер, но для простоты оставим так
                CurrentViewModel = Activator.CreateInstance(viewModelType);
            }
            else if (parameter is string viewModelName)
            {
                // Альтернативный вариант: навигация по имени (как в вашем примере)
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
                        // Приветственная страница
                        CurrentViewModel = null;
                        break;
                }
            }
        }
    }
}
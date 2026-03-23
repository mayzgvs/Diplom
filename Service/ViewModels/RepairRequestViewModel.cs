using Service.Data;
using Service.Models;
using Service.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Service.ViewModels
{
    public class RepairRequestViewModel : BaseViewModel
    {
        private readonly RepairRequestModel _model = new RepairRequestModel();

        public ObservableCollection<RepairRequest> RepairRequests { get; private set; }
        public ObservableCollection<RepairRequest> FilteredRequests { get; private set; }

        private RepairRequest _selectedRepairRequest;
        public RepairRequest SelectedRepairRequest
        {
            get => _selectedRepairRequest;
            set { _selectedRepairRequest = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); Filter(); }
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public RepairRequestViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddRepairRequest());
            EditCommand = new RelayCommand(_ => EditRepairRequest(), _ => SelectedRepairRequest != null);
            DeleteCommand = new RelayCommand(_ => DeleteRepairRequest(), _ => SelectedRepairRequest != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetRepairRequests();
            RepairRequests = new ObservableCollection<RepairRequest>(list);
            FilteredRequests = new ObservableCollection<RepairRequest>(list);
        }

        private void Filter()
        {
            if (RepairRequests == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? RepairRequests.ToList()
                : RepairRequests.Where(r =>
                    (r.Car?.RegistrationNumber?.ToLower().Contains(SearchText.ToLower()) == true) ||
                    (r.Client?.ToLower().Contains(SearchText.ToLower()) == true)).ToList();

            FilteredRequests = new ObservableCollection<RepairRequest>(filtered);
        }

        private void AddRepairRequest()
        {
            var window = new AddRepairView();
            window.DataContext = new AddRepairRequestViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditRepairRequest()
        {
            var window = new AddRepairView();
            window.DataContext = new AddRepairRequestViewModel(SelectedRepairRequest);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteRepairRequest()
        {
            if (MessageBox.Show($"Удалить заявку #{SelectedRepairRequest.Id}?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteRepairRequest(SelectedRepairRequest);
                LoadData();
            }
        }
    }
}
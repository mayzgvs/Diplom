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
    public class WorkItemViewModel : BaseViewModel
    {
        private readonly WorkItemModel _model = new WorkItemModel();

        public ObservableCollection<RepairRequest> RepairRequests { get; private set; }
        public ObservableCollection<WorkItem> WorkItems { get; private set; }

        private RepairRequest _selectedRepairRequest;
        public RepairRequest SelectedRepairRequest
        {
            get => _selectedRepairRequest;
            set
            {
                _selectedRepairRequest = value;
                OnPropertyChanged();
                LoadWorkItems();
                OnPropertyChanged(nameof(CanAddWorkItem));
            }
        }

        private WorkItem _selectedWorkItem;
        public WorkItem SelectedWorkItem
        {
            get => _selectedWorkItem;
            set { _selectedWorkItem = value; OnPropertyChanged(); }
        }

        public bool CanAddWorkItem => SelectedRepairRequest != null;

        public ICommand LoadedCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public WorkItemViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            RefreshCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddWorkItem(), _ => CanAddWorkItem);
            EditCommand = new RelayCommand(_ => EditWorkItem(), _ => SelectedWorkItem != null);
            DeleteCommand = new RelayCommand(_ => DeleteWorkItem(), _ => SelectedWorkItem != null);

            LoadData();
        }

        private void LoadData()
        {
            RepairRequests = new ObservableCollection<RepairRequest>(_model.GetRepairRequests());
            OnPropertyChanged(nameof(RepairRequests));
        }

        private void LoadWorkItems()
        {
            if (SelectedRepairRequest != null)
            {
                var workItems = _model.GetWorkItemsByRequestId(SelectedRepairRequest.Id);

                WorkItems = new ObservableCollection<WorkItem>(workItems);
            }
            else
            {
                WorkItems = new ObservableCollection<WorkItem>();
            }

            OnPropertyChanged(nameof(WorkItems));
        }

        private void AddWorkItem()
        {
            var window = new AddWorkItemView();
            var viewModel = new AddWorkItemViewModel(SelectedRepairRequest);
            window.DataContext = viewModel;

            viewModel.WorkItemSaved += (s, e) => LoadWorkItems();

            if (window.ShowDialog() == true)
            {
                LoadWorkItems();
                UpdateRequestTotalCost();
            }
        }

        private void EditWorkItem()
        {
            var window = new AddWorkItemView();
            var viewModel = new AddWorkItemViewModel(SelectedRepairRequest, SelectedWorkItem);
            window.DataContext = viewModel;

            viewModel.WorkItemSaved += (s, e) => LoadWorkItems();

            if (window.ShowDialog() == true)
            {
                LoadWorkItems();
                UpdateRequestTotalCost();
            }
        }

        private void DeleteWorkItem()
        {
            if (MessageBox.Show($"Удалить работу '{SelectedWorkItem.ServiceName}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _model.DeleteWorkItem(SelectedWorkItem);
                LoadWorkItems();
                UpdateRequestTotalCost();
            }
        }

        private void UpdateRequestTotalCost()
        {
            if (SelectedRepairRequest != null)
            {
                var totalCost = _model.CalculateTotalCost(SelectedRepairRequest.Id);
                _model.UpdateRequestTotalCost(SelectedRepairRequest.Id, totalCost);
                SelectedRepairRequest.TotalCost = totalCost;
                OnPropertyChanged(nameof(SelectedRepairRequest));
            }
        }
    }
}
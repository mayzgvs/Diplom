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
    public class ConsumableViewModel : BaseViewModel
    {
        private readonly ConsumableModel _model = new ConsumableModel();

        public ObservableCollection<Consumable> Consumables { get; private set; }
        public ObservableCollection<Consumable> FilteredConsumables { get; private set; }

        private Consumable _selectedConsumable;
        public Consumable SelectedConsumable
        {
            get => _selectedConsumable;
            set { _selectedConsumable = value; OnPropertyChanged(); }
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

        public ConsumableViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddConsumable());
            EditCommand = new RelayCommand(_ => EditConsumable(), _ => SelectedConsumable != null);
            DeleteCommand = new RelayCommand(_ => DeleteConsumable(), _ => SelectedConsumable != null);

            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetConsumables();
            Consumables = new ObservableCollection<Consumable>(list);
            FilteredConsumables = new ObservableCollection<Consumable>(list);
        }
        private void Filter()
        {
            if (Consumables == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Consumables.ToList()
                : Consumables.Where(c =>
                    c.Name?.ToLower().Contains(SearchText.ToLower()) == true).ToList();

            FilteredConsumables = new ObservableCollection<Consumable>(filtered);
        }

        private void AddConsumable()
        {
            var window = new AddConsumablesView();
            window.DataContext = new AddConsumablesViewModel();
            if (window.ShowDialog() == true) LoadData();
        }

        private void EditConsumable()
        {
            var window = new AddConsumablesView();
            window.DataContext = new AddConsumablesViewModel(SelectedConsumable);
            if (window.ShowDialog() == true) LoadData();
        }

        private void DeleteConsumable()
        {
            if (MessageBox.Show($"Удалить расходник '{SelectedConsumable.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _model.DeleteConsumable(SelectedConsumable);
                LoadData();
            }
        }
    }
}
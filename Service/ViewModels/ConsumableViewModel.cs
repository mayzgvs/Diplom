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
            set
            {
                _searchText = value;
                OnPropertyChanged();
                Filter();
            }
        }

        private ConsumablesCategory _selectedCategory;
        public ConsumablesCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                Filter();
            }
        }

        public ObservableCollection<ConsumablesCategory> Categories { get; private set; }

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public ConsumableViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadData());
            AddCommand = new RelayCommand(_ => AddConsumable());
            EditCommand = new RelayCommand(_ => EditConsumable(), _ => SelectedConsumable != null);
            DeleteCommand = new RelayCommand(_ => DeleteConsumable(), _ => SelectedConsumable != null);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            Consumables = new ObservableCollection<Consumable>();
            FilteredConsumables = new ObservableCollection<Consumable>();
            Categories = new ObservableCollection<ConsumablesCategory>();
            LoadData();
        }

        private void LoadData()
        {
            var list = _model.GetConsumables();
            var categories = _model.GetCategories();

            Consumables.Clear();
            foreach (var consumable in list)
            {
                Consumables.Add(consumable);
            }

            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            Filter();

            OnPropertyChanged(nameof(Consumables));
            OnPropertyChanged(nameof(Categories));
        }

        private void Filter()
        {
            if (Consumables == null) return;

            var filtered = Consumables.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lowerSearch = SearchText.Trim().ToLower();
                filtered = filtered.Where(c =>
                    c.Name?.ToLower().Contains(lowerSearch) == true
                );
            }

            if (SelectedCategory != null)
            {
                filtered = filtered.Where(c => c.ConsumableCategoryId == SelectedCategory.Id);
            }

            FilteredConsumables.Clear();
            foreach (var consumable in filtered)
            {
                FilteredConsumables.Add(consumable);
            }

            OnPropertyChanged(nameof(FilteredConsumables));
        }

        private void AddConsumable()
        {
            var window = new AddConsumablesView();
            var viewModel = new AddConsumablesViewModel();

            viewModel.ConsumableSaved += OnConsumableSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void EditConsumable()
        {
            var window = new AddConsumablesView();
            var viewModel = new AddConsumablesViewModel(SelectedConsumable);

            viewModel.ConsumableSaved += OnConsumableSaved;
            window.DataContext = viewModel;

            window.ShowDialog();
        }

        private void OnConsumableSaved(object sender, EventArgs e)
        {
            LoadData();

            if (sender is AddConsumablesViewModel viewModel)
            {
                viewModel.ConsumableSaved -= OnConsumableSaved;
            }
        }

        private void DeleteConsumable()
        {
            if (SelectedConsumable == null) return;

            if (MessageBox.Show($"Удалить расходник '{SelectedConsumable.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _model.DeleteConsumable(SelectedConsumable);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
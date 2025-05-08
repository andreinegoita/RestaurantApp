using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;

        private ObservableCollection<Order> _allOrders;
        private ObservableCollection<Order> _activeOrders;
        private ObservableCollection<Product> _lowStockProducts;
        private Order _selectedOrder;
        private bool _isLoading;

        public ObservableCollection<Order> AllOrders
        {
            get => _allOrders;
            set => SetProperty(ref _allOrders, value);
        }

        public ObservableCollection<Order> ActiveOrders
        {
            get => _activeOrders;
            set => SetProperty(ref _activeOrders, value);
        }

        public ObservableCollection<Product> LowStockProducts
        {
            get => _lowStockProducts;
            set => SetProperty(ref _lowStockProducts, value);
        }

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand ManageCategoriesCommand { get; }
        public ICommand ManageProductsCommand { get; }
        public ICommand ManageMenusCommand { get; }
        public ICommand ManageAllergensCommand { get; }
        public ICommand ViewAllOrdersCommand { get; }
        public ICommand ViewActiveOrdersCommand { get; }
        public ICommand ViewLowStockCommand { get; }
        public ICommand UpdateOrderStatusCommand { get; }

        public AdminDashboardViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            AllOrders = new ObservableCollection<Order>();
            ActiveOrders = new ObservableCollection<Order>();
            LowStockProducts = new ObservableCollection<Product>();

            LogoutCommand = new RelayCommand(Logout);
            RefreshDataCommand = new AsyncRelayCommand(LoadDataAsync);
            ManageCategoriesCommand = new RelayCommand(() => _navigationService.NavigateTo<CategoryManagementViewModel>());
            //ManageProductsCommand = new RelayCommand(() => _navigationService.NavigateTo<ProductManagementViewModel>());
            //ManageMenusCommand = new RelayCommand(() => _navigationService.NavigateTo<MenuManagementViewModel>());
            //ManageAllergensCommand = new RelayCommand(() => _navigationService.NavigateTo<AllergenManagementViewModel>());
            ViewAllOrdersCommand = new AsyncRelayCommand(LoadAllOrdersAsync);
            ViewActiveOrdersCommand = new AsyncRelayCommand(LoadActiveOrdersAsync);
            ViewLowStockCommand = new AsyncRelayCommand(LoadLowStockProductsAsync);
            //UpdateOrderStatusCommand = new AsyncRelayCommand<int>(UpdateOrderStatusAsync);

            Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                await Task.WhenAll(
                    LoadAllOrdersAsync(),
                    LoadActiveOrdersAsync(),
                    LoadLowStockProductsAsync()
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAllOrdersAsync()
        {
            var orders = await _dataService.GetAllOrdersAsync();
            AllOrders = new ObservableCollection<Order>(orders);
        }

        private async Task LoadActiveOrdersAsync()
        {
            var orders = await _dataService.GetActiveOrdersAsync();
            ActiveOrders = new ObservableCollection<Order>(orders);
        }

        private async Task LoadLowStockProductsAsync()
        {
            var products = await _dataService.GetLowStockProductsAsync();
            LowStockProducts = new ObservableCollection<Product>(products);
        }

        private async Task UpdateOrderStatusAsync(int statusId)
        {
            if (SelectedOrder == null)
                return;

            try
            {
                IsLoading = true;
                await _dataService.UpdateOrderStatusAsync(SelectedOrder.OrderId, statusId);

                await LoadActiveOrdersAsync();
                await LoadAllOrdersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating order status: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Logout()
        {
            AppSession.CurrentUser = null;
            _navigationService.NavigateTo<HomeViewModel>();
        }
    }
}
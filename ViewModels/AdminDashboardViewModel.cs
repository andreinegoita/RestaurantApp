using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
            ManageProductsCommand = new RelayCommand(() => _navigationService.NavigateTo<ProductManagementViewModel>());
            ManageMenusCommand = new RelayCommand(() => _navigationService.NavigateTo<MenuManagementViewModel>());
            ManageAllergensCommand = new RelayCommand(() => _navigationService.NavigateTo<AllergenManagementViewModel>());
            ViewAllOrdersCommand = new AsyncRelayCommand(LoadAllOrdersAsync);
            ViewActiveOrdersCommand = new AsyncRelayCommand(LoadActiveOrdersAsync);
            ViewLowStockCommand = new AsyncRelayCommand(LoadLowStockProductsAsync);
            UpdateOrderStatusCommand = new AsyncRelayCommand<object>(UpdateOrderStatusAsync);

            Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                try
                {
                    await LoadAllOrdersAsync();
                    System.Diagnostics.Debug.WriteLine($"All orders loaded: {AllOrders.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading all orders: {ex.Message}");
                }

                try
                {
                    await LoadActiveOrdersAsync();
                    System.Diagnostics.Debug.WriteLine($"Active orders loaded: {ActiveOrders.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading active orders: {ex.Message}");
                }

                try
                {
                    await LoadLowStockProductsAsync();
                    System.Diagnostics.Debug.WriteLine($"Low stock products loaded: {LowStockProducts.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading low stock products: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error loading data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("Finished loading dashboard data");
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

        private async Task UpdateOrderStatusAsync(object parameter)
        {
            if (SelectedOrder == null)
                return;

            int statusId;

            try
            {
                if (parameter is int i)
                {
                    statusId = i;
                }
                else if (parameter is string s && int.TryParse(s, out var parsed))
                {
                    statusId = parsed;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Invalid parameter for UpdateOrderStatusCommand");
                    return;
                }

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
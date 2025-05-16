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
        public class CustomerOrdersViewModel : ViewModelBase
        {
            private readonly INavigationService _navigationService;
            private readonly IDataService _dataService;
            private ObservableCollection<OrderViewModel> _orders;
            private ObservableCollection<OrderViewModel> _activeOrders;
            private OrderViewModel _selectedOrder;
            private bool _isLoading;
            private string _errorMessage;

            public ObservableCollection<OrderViewModel> Orders
            {
                get => _orders;
                set => SetProperty(ref _orders, value);
            }

            public ObservableCollection<OrderViewModel> ActiveOrders
            {
                get => _activeOrders;
                set => SetProperty(ref _activeOrders, value);
            }

            public OrderViewModel SelectedOrder
            {
                get => _selectedOrder;
                set => SetProperty(ref _selectedOrder, value);
            }

            public bool IsLoading
            {
                get => _isLoading;
                set => SetProperty(ref _isLoading, value);
            }

            public string ErrorMessage
            {
                get => _errorMessage;
                set => SetProperty(ref _errorMessage, value);
            }

            public ICommand RefreshCommand { get; }
            public ICommand CancelOrderCommand { get; }
            public ICommand ViewOrderDetailsCommand { get; }
            public ICommand GoBackCommand { get; }

        public CustomerOrdersViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            Orders = new ObservableCollection<OrderViewModel>();
            ActiveOrders = new ObservableCollection<OrderViewModel>();

            RefreshCommand = new RelayCommand(async () => await LoadOrdersAsync());

            CancelOrderCommand = new RelayCommand<OrderViewModel>(
                async order => await CancelOrderAsync(order),
                order => order != null && order.CanBeCancelled
            );

            ViewOrderDetailsCommand = new RelayCommand<OrderViewModel>(
                order => ViewOrderDetails(order),
                order => order != null
            );

            GoBackCommand = new RelayCommand(() => _navigationService.NavigateTo<MenuViewModel>());

            Task.Run(async () => await LoadOrdersAsync());
        }

        private async Task LoadOrdersAsync()
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;

                    if (AppSession.CurrentUser == null)
                    {
                        ErrorMessage = "You must be logged in to view your orders.";
                        return;
                    }

                    var userOrders = await _dataService.GetUserOrdersAsync(AppSession.CurrentUser.UserId);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Orders.Clear();
                        ActiveOrders.Clear();

                        foreach (var order in userOrders)
                        {
                            var orderViewModel = new OrderViewModel(order);
                            Orders.Add(orderViewModel);

                            if (order.StatusId != 4 && order.StatusId != 5) 
                            {
                                ActiveOrders.Add(orderViewModel);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error loading orders: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }

            private async Task CancelOrderAsync(OrderViewModel order)
            {
                if (order == null)
                    return;

                try
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;

                    await _dataService.UpdateOrderStatusAsync(order.OrderId, 5); 
                    await LoadOrdersAsync();

                    MessageBox.Show("Order cancelled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error cancelling order: {ex.Message}";
                    MessageBox.Show($"Failed to cancel order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }

            private void ViewOrderDetails(OrderViewModel order)
            {
                if (order == null)
                    return;

                AppSession.SelectedOrder = order.OriginalOrder;
                _navigationService.NavigateTo<OrderDetailsViewModel>();
            }
        }


    public class OrderViewModel : ViewModelBase
    {
        public int OrderId { get; }
        public string OrderCode { get; }
        public DateTime OrderDate { get; }
        public DateTime EstimatedDeliveryTime { get; set;
        }
        public string StatusName { get; }
        public decimal SubTotal { get; }
        public decimal DeliveryFee { get; }
        public decimal DiscountAmount { get; }
        public decimal TotalAmount { get; }
        public List<OrderItemViewModel> Items { get; }
        public bool CanBeCancelled { get; }
        public Order OriginalOrder { get; }

        public OrderViewModel(Order order)
        {
            OriginalOrder = order;
            OrderId = order.OrderId;
            OrderCode = order.OrderCode;
            OrderDate = order.OrderDate;
            EstimatedDeliveryTime = (DateTime)order.EstimatedDeliveryTime;
            StatusName = order.Status?.Name ?? "Unknown";

            SubTotal = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity);
            DeliveryFee = order.DeliveryFee;
            DiscountAmount = order.DiscountAmount;
            TotalAmount = SubTotal + DeliveryFee - DiscountAmount;

            Items = order.OrderItems.Select(item => new OrderItemViewModel(item)).ToList();

            CanBeCancelled = order.StatusId == 1 || order.StatusId == 2;
        }
    }

    public class OrderItemViewModel : ViewModelBase
    {
        public string Name { get; }
        public int Quantity { get; }
        public decimal UnitPrice { get; }
        public decimal TotalPrice { get; }
        public bool IsMenu { get; }

        public OrderItemViewModel(OrderItem item)
        {
            Quantity = item.Quantity;
            UnitPrice = item.UnitPrice;
            TotalPrice = item.Quantity * item.UnitPrice;

            if (item.Product != null)
            {
                Name = item.Product.Name;
                IsMenu = false;
            }
            else if (item.Menu != null)
            {
                Name = item.Menu.Name;
                IsMenu = true;
            }
            else
            {
                Name = "Unknown Item";
                IsMenu = false;
            }
        }
    }

}
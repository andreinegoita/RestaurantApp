using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class OrderDetailsViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private Order _order;
        private bool _isLoading;
        private string _errorMessage;
        private bool _isEmployee;
        private OrderStatus _selectedStatus;
        private ObservableCollection<OrderStatus> _availableStatuses;

        public Order Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public string OrderCode => Order?.OrderCode;
        public DateTime OrderDate => Order?.OrderDate ?? DateTime.MinValue;
        public DateTime EstimatedDeliveryTime => Order?.EstimatedDeliveryTime ?? DateTime.MinValue;
        public string StatusName => Order?.Status?.Name;
        public string CustomerName => Order?.User != null ? $"{Order.User.FirstName} {Order.User.LastName}" : "N/A";
        public string CustomerPhone => Order?.User?.PhoneNumber ?? "N/A";
        public string DeliveryAddress => Order?.User?.DeliveryAddress ?? "N/A";

        public decimal SubTotal => Order?.OrderItems?.Sum(item => item.UnitPrice * item.Quantity) ?? 0;
        public decimal DeliveryFee => Order?.DeliveryFee ?? 0;
        public decimal DiscountAmount => Order?.DiscountAmount ?? 0;
        public decimal TotalAmount => SubTotal + DeliveryFee - DiscountAmount;

        public ObservableCollection<OrderItemViewModel> OrderItems { get; private set; }

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

        public bool IsEmployee
        {
            get => _isEmployee;
            set => SetProperty(ref _isEmployee, value);
        }

        public ObservableCollection<OrderStatus> AvailableStatuses
        {
            get => _availableStatuses;
            set => SetProperty(ref _availableStatuses, value);
        }

        public OrderStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value) && value != null && Order != null && Order.StatusId != value.StatusId)
                {
                    UpdateOrderStatusAsync(value.StatusId);
                }
            }
        }

        public ICommand GoBackCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrderDetailsViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            OrderItems = new ObservableCollection<OrderItemViewModel>();
            AvailableStatuses = new ObservableCollection<OrderStatus>();

            GoBackCommand = new RelayCommand(() => _navigationService.NavigateTo<CustomerOrdersViewModel>());
            CancelOrderCommand = new RelayCommand(async () => await CancelOrderAsync(), CanCancelOrder);
            RefreshCommand = new RelayCommand(async () => await LoadOrderDetailsAsync());

            IsEmployee = AppSession.CurrentUser?.RoleId == 2; // Assuming RoleId 2 is for employees

            LoadOrderDetailsAsync();
        }

        private async Task LoadOrderDetailsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (AppSession.SelectedOrder == null)
                {
                    ErrorMessage = "No order selected.";
                    return;
                }

                Order = await _dataService.GetOrderByIdAsync(AppSession.SelectedOrder.OrderId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    OrderItems.Clear();
                    foreach (var item in Order.OrderItems)
                    {
                        OrderItems.Add(new OrderItemViewModel(item));
                    }

                    if (IsEmployee)
                    {
                        LoadAvailableStatuses();
                    }

                    OnPropertyChanged(nameof(OrderCode));
                    OnPropertyChanged(nameof(OrderDate));
                    OnPropertyChanged(nameof(EstimatedDeliveryTime));
                    OnPropertyChanged(nameof(StatusName));
                    OnPropertyChanged(nameof(CustomerName));
                    OnPropertyChanged(nameof(CustomerPhone));
                    OnPropertyChanged(nameof(DeliveryAddress));
                    OnPropertyChanged(nameof(SubTotal));
                    OnPropertyChanged(nameof(DeliveryFee));
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(TotalAmount));
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading order details: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadAvailableStatuses()
        {
            AvailableStatuses.Clear();

            AvailableStatuses.Add(new OrderStatus { StatusId = 1, Name = "Registered" });
            AvailableStatuses.Add(new OrderStatus { StatusId = 2, Name = "Preparing" });
            AvailableStatuses.Add(new OrderStatus { StatusId = 3, Name = "Out for Delivery" });
            AvailableStatuses.Add(new OrderStatus { StatusId = 4, Name = "Delivered" });
            AvailableStatuses.Add(new OrderStatus { StatusId = 5, Name = "Cancelled" });

            SelectedStatus = AvailableStatuses.FirstOrDefault(s => s.StatusId == Order.StatusId);
        }

        private bool CanCancelOrder()
        {
            return Order != null && (Order.StatusId == 1 || Order.StatusId == 2);
        }

        private async Task CancelOrderAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _dataService.UpdateOrderStatusAsync(Order.OrderId, 5); 
                await LoadOrderDetailsAsync();

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

        private async Task UpdateOrderStatusAsync(int statusId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _dataService.UpdateOrderStatusAsync(Order.OrderId, statusId);
                await LoadOrderDetailsAsync();

                MessageBox.Show("Order status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating order status: {ex.Message}";
                MessageBox.Show($"Failed to update order status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                SelectedStatus = AvailableStatuses.FirstOrDefault(s => s.StatusId == Order.StatusId);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
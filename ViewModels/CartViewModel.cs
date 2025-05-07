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
    public class CartViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private ObservableCollection<CartItemViewModel> _cartItems;
        private decimal _subtotal;
        private decimal _shippingCost;
        private decimal _discount;
        private decimal _total;
        private bool _isCheckingOut;
        private string _errorMessage;
        private readonly AppConfiguration _configuration;

        public ObservableCollection<CartItemViewModel> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }

        public decimal ShippingCost
        {
            get => _shippingCost;
            set => SetProperty(ref _shippingCost, value);
        }

        public decimal Discount
        {
            get => _discount;
            set => SetProperty(ref _discount, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        public bool IsCheckingOut
        {
            get => _isCheckingOut;
            set => SetProperty(ref _isCheckingOut, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand ContinueShoppingCommand { get; }

        public CartViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;
            _configuration = dataService.GetConfiguration();

            CartItems = new ObservableCollection<CartItemViewModel>();

            IncreaseQuantityCommand = new RelayCommand<int>(IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<int>(DecreaseQuantity);
            RemoveItemCommand = new RelayCommand<int>(RemoveItem);
            ClearCartCommand = new RelayCommand(ClearCart);
            CheckoutCommand = new AsyncRelayCommand(CheckoutAsync, CanCheckout);
            ContinueShoppingCommand = new RelayCommand(() => _navigationService.NavigateTo<MenuViewModel>());

            LoadCart();

            CartService.CartChanged += (s, e) => LoadCart();
        }

        private void LoadCart()
        {
            CartItems.Clear();

            for (int i = 0; i < CartService.Items.Count; i++)
            {
                var item = CartService.Items[i];
                CartItems.Add(new CartItemViewModel
                {
                    Index = i,
                    ProductId = item.ProductId,
                    MenuId = item.MenuId,
                    Name = item.Name,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice
                });
            }

            CalculateTotals();
        }

        private void CalculateTotals()
        {
            Subtotal = CartService.TotalAmount;

            ShippingCost = Subtotal >= _configuration.MinOrderValueForFreeShipping ? 0 : _configuration.ShippingCost;

            Discount = Subtotal >= _configuration.MinOrderValueForDiscount ?
                       Math.Round(Subtotal * (_configuration.OrderDiscountPercentage / 100), 2) : 0;

            Total = Subtotal + ShippingCost - Discount;
        }

        private void IncreaseQuantity(int index)
        {
            if (index >= 0 && index < CartService.Items.Count)
            {
                CartService.UpdateQuantity(index, CartService.Items[index].Quantity + 1);
            }
        }

        private void DecreaseQuantity(int index)
        {
            if (index >= 0 && index < CartService.Items.Count)
            {
                if (CartService.Items[index].Quantity > 1)
                {
                    CartService.UpdateQuantity(index, CartService.Items[index].Quantity - 1);
                }
                else
                {
                    RemoveItem(index);
                }
            }
        }

        private void RemoveItem(int index)
        {
            if (index >= 0 && index < CartService.Items.Count)
            {
                CartService.RemoveItem(index);
            }
        }

        private void ClearCart()
        {
            CartService.ClearCart();
        }

        private bool CanCheckout()
        {
            return CartService.Items.Count > 0 && !IsCheckingOut && AppSession.CurrentUser != null;
        }

        private async Task CheckoutAsync()
        {
            if (CartService.Items.Count == 0)
            {
                MessageBox.Show("Your cart is empty.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Please log in to complete your order.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Information);
                _navigationService.NavigateTo<LoginViewModel>();
                return;
            }

            try
            {
                IsCheckingOut = true;
                ErrorMessage = string.Empty;

                var order = new Order
                {
                    OrderCode = GenerateOrderCode(),
                    OrderDate = DateTime.Now,
                    UserId = AppSession.CurrentUser.UserId,
                    StatusId = 1 
                };

                var orderItems = CartService.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    MenuId = item.MenuId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList();

                int orderId = await _dataService.CreateOrderAsync(order, orderItems);

                CartService.ClearCart();

                MessageBox.Show($"Order placed successfully! Your order code is: {order.OrderCode}",
                    "Order Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);

                _navigationService.NavigateTo<CustomerOrdersViewModel>();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to place order: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsCheckingOut = false;
            }
        }

        private string GenerateOrderCode()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }

    public class CartItemViewModel : ViewModelBase
    {
        public int Index { get; set; }
        public int? ProductId { get; set; }
        public int? MenuId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
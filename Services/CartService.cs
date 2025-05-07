using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Services
{
    public static class CartService
    {
        private static ObservableCollection<CartItem> _items = new ObservableCollection<CartItem>();

        public static event EventHandler CartChanged;

        public static ObservableCollection<CartItem> Items => _items;

        public static decimal TotalAmount => _items.Sum(i => i.UnitPrice * i.Quantity);

        public static int ItemCount => _items.Sum(i => (int)i.Quantity);

        public static void AddProduct(int productId, string name, decimal price, int quantity)
        {
            var existingItem = _items.FirstOrDefault(i => i.ProductId == productId && i.MenuId == null);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _items.Add(new CartItem
                {
                    ProductId = productId,
                    Name = name,
                    UnitPrice = price,
                    Quantity = quantity
                });
            }

            OnCartChanged();
        }

        public static void AddMenu(int menuId, string name, decimal price, int quantity)
        {
            var existingItem = _items.FirstOrDefault(i => i.MenuId == menuId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _items.Add(new CartItem
                {
                    MenuId = menuId,
                    Name = name,
                    UnitPrice = price,
                    Quantity = quantity
                });
            }

            OnCartChanged();
        }

        public static void UpdateQuantity(int index, int quantity)
        {
            if (index >= 0 && index < _items.Count)
            {
                if (quantity <= 0)
                {
                    _items.RemoveAt(index);
                }
                else
                {
                    _items[index].Quantity = quantity;
                }

                OnCartChanged();
            }
        }

        public static void RemoveItem(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
                OnCartChanged();
            }
        }

        public static void ClearCart()
        {
            _items.Clear();
            OnCartChanged();
        }

        private static void OnCartChanged()
        {
            CartChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        private int? _productId;
        private int? _menuId;
        private string _name;
        private decimal _unitPrice;
        private int _quantity;

        public int? ProductId
        {
            get => _productId;
            set
            {
                _productId = value;
                OnPropertyChanged();
            }
        }

        public int? MenuId
        {
            get => _menuId;
            set
            {
                _menuId = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

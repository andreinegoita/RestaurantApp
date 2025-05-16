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
    public class MenuViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private ObservableCollection<CategoryViewModel> _categories;
        private string _searchKeyword;
        private bool _includeAllergen;
        private bool _excludeAllergen;
        private bool _isSearching;
        private bool _isLoading;
        private string _errorMessage;

        public ObservableCollection<CategoryViewModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public bool IncludeAllergen
        {
            get => _includeAllergen;
            set => SetProperty(ref _includeAllergen, value);
        }

        public bool ExcludeAllergen
        {
            get => _excludeAllergen;
            set
            {
                if (SetProperty(ref _excludeAllergen, value) && value && IncludeAllergen)
                {
                    IncludeAllergen = false;
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
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

        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand BackCommand { get; }

        public MenuViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;
            Categories = new ObservableCollection<CategoryViewModel>();

            SearchCommand = new AsyncRelayCommand(SearchMenuItemsAsync);
            ClearSearchCommand = new RelayCommand(LoadAllMenuItems);
            AddToCartCommand = new RelayCommand<object>(AddItemToCart);
            BackCommand = new RelayCommand(() => _navigationService.NavigateTo<CustomerDashboardViewModel>());

            LoadAllMenuItems();
        }

        private void LoadAllMenuItems()
        {
            IsSearching = false;
            SearchKeyword = string.Empty;
            IncludeAllergen = false;
            ExcludeAllergen = false;
            LoadMenu();
        }

        private async void LoadMenu()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categories = await _dataService.GetCategoriesAsync();
                var products = await _dataService.GetProductsAsync();
                var menus = await _dataService.GetMenusAsync();
                var menuConfiguration = _dataService.GetConfiguration();

                var categoryViewModels = new List<CategoryViewModel>();

                foreach (var category in categories)
                {
                    var categoryVM = new CategoryViewModel
                    {
                        CategoryId = category.CategoryId,
                        Name = category.Name,
                        Products = new ObservableCollection<ProductViewModel>(),
                        Menus = new ObservableCollection<MenuItemViewModel>()
                    };

                    var categoryProducts = products.Where(p => p.CategoryId == category.CategoryId).ToList();
                    foreach (var product in categoryProducts)
                    {
                        categoryVM.Products.Add(new ProductViewModel
                        {
                            ProductId = product.ProductId,
                            Name = product.Name,
                            Price = product.Price,
                            PortionQuantity = product.PortionQuantity,
                            Unit = product.Unit,
                            TotalQuantity = product.TotalQuantity,
                            IsAvailable = product.TotalQuantity > 0,
                            CategoryName = category.Name,
                            Images = new ObservableCollection<string>(product.ProductImages?.Select(pi => pi.ImageUrl) ?? new List<string>()),
                            Allergens = new ObservableCollection<string>(product.ProductAllergens?.Select(pa => pa.Allergen.Name) ?? new List<string>())
                        });
                    }

                    var categoryMenus = menus.Where(m => m.CategoryId == category.CategoryId).ToList();
                    foreach (var menu in categoryMenus)
                    {
                        bool isMenuAvailable = menu.MenuProducts.All(mp => mp.Product.TotalQuantity > 0);

                        decimal originalPrice = menu.MenuProducts.Sum(mp => mp.Product.Price);
                        decimal discountedPrice = originalPrice * (1 - (menuConfiguration.MenuDiscountPercentage / 100));

                        var menuVM = new MenuItemViewModel
                        {
                            MenuId = menu.MenuId,
                            Name = menu.Name,
                            Price = Math.Round(discountedPrice, 2),
                            OriginalPrice = Math.Round(originalPrice, 2),
                            IsAvailable = isMenuAvailable,
                            CategoryName = category.Name,
                            Products = new ObservableCollection<MenuProductViewModel>()
                        };

                        foreach (var menuProduct in menu.MenuProducts)
                        {
                            menuVM.Products.Add(new MenuProductViewModel
                            {
                                ProductName = menuProduct.Product.Name,
                                Quantity = menuProduct.Quantity,
                                Unit = menuProduct.Product.Unit,
                                Images = new ObservableCollection<string>(menuProduct.Product.ProductImages?.Select(pi => pi.ImageUrl) ?? new List<string>()),
                                Allergens = new ObservableCollection<string>(menuProduct.Product.ProductAllergens?.Select(pa => pa.Allergen.Name) ?? new List<string>())
                            });

                            foreach (var allergen in menuProduct.Product.ProductAllergens?.Select(pa => pa.Allergen.Name) ?? new List<string>())
                            {
                                if (!menuVM.Allergens.Contains(allergen))
                                {
                                    menuVM.Allergens.Add(allergen);
                                }
                            }
                        }

                        categoryVM.Menus.Add(menuVM);
                    }

                    if (categoryVM.Products.Count > 0 || categoryVM.Menus.Count > 0)
                    {
                        categoryViewModels.Add(categoryVM);
                    }
                }

                Categories = new ObservableCollection<CategoryViewModel>(categoryViewModels);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading menu: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchMenuItemsAsync()
        {
            try
            {
                IsLoading = true;
                IsSearching = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(SearchKeyword) && !IncludeAllergen && !ExcludeAllergen)
                {
                    LoadAllMenuItems();
                    return;
                }

                var searchResults = await _dataService.SearchProductsAsync(SearchKeyword, IncludeAllergen, ExcludeAllergen);
                var categoryViewModels = new Dictionary<int, CategoryViewModel>();

                foreach (var product in searchResults)
                {
                    if (!categoryViewModels.TryGetValue(product.CategoryId, out var categoryVM))
                    {
                        categoryVM = new CategoryViewModel
                        {
                            CategoryId = product.CategoryId,
                            Name = product.Category.Name,
                            Products = new ObservableCollection<ProductViewModel>(),
                            Menus = new ObservableCollection<MenuItemViewModel>()
                        };
                        categoryViewModels[product.CategoryId] = categoryVM;
                    }

                    categoryVM.Products.Add(new ProductViewModel
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        PortionQuantity = product.PortionQuantity,
                        Unit = product.Unit,
                        TotalQuantity = product.TotalQuantity,
                        IsAvailable = product.TotalQuantity > 0,
                        CategoryName = product.Category.Name,
                        Images = new ObservableCollection<string>(product.ProductImages?.Select(pi => pi.ImageUrl) ?? new List<string>()),
                        Allergens = new ObservableCollection<string>(product.ProductAllergens?.Select(pa => pa.Allergen.Name) ?? new List<string>())
                    });
                }

                Categories = new ObservableCollection<CategoryViewModel>(categoryViewModels.Values);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddItemToCart(object parameter)
        {
            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Please log in to add items to your cart.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (parameter is ProductViewModel product)
                {
                    if (!product.IsAvailable)
                    {
                        MessageBox.Show("This product is currently unavailable.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    CartService.AddProduct(product.ProductId, product.Name, product.Price, 1);
                    MessageBox.Show($"{product.Name} added to cart!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (parameter is MenuItemViewModel menu)
                {
                    if (!menu.IsAvailable)
                    {
                        MessageBox.Show("This menu is currently unavailable.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    CartService.AddMenu(menu.MenuId, menu.Name, menu.Price, 1);
                    MessageBox.Show($"{menu.Name} added to cart!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding item to cart: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class CategoryViewModel : ViewModelBase
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public ObservableCollection<ProductViewModel> Products { get; set; }
        public ObservableCollection<MenuItemViewModel> Menus { get; set; }
    }

    public class ProductViewModel : ViewModelBase
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal PortionQuantity { get; set; }
        public string Unit { get; set; }
        public decimal TotalQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string CategoryName { get; set; }
        public ObservableCollection<string> Images { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Allergens { get; set; } = new ObservableCollection<string>();
    }

    public class MenuItemViewModel : ViewModelBase
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string CategoryName { get; set; }
        public ObservableCollection<MenuProductViewModel> Products { get; set; } = new ObservableCollection<MenuProductViewModel>();
        public ObservableCollection<string> Allergens { get; set; } = new ObservableCollection<string>();
    }

    public class MenuProductViewModel : ViewModelBase
    {
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public ObservableCollection<string> Images { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Allergens { get; set; } = new ObservableCollection<string>();
    }
}
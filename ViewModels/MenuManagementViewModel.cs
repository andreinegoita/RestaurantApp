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
    public class MenuManagementViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private readonly AppConfiguration _configuration;

        private ObservableCollection<Menu> _menus;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private ObservableCollection<MenuProduct> _selectedMenuProducts;

        private Menu _selectedMenu;
        private Menu _editingMenu;
        private Category _selectedCategory;
        private Product _selectedProduct;

        private bool _isLoading;
        private bool _isEditing;
        private bool _isAddingNew;
        private string _errorMessage;
        private decimal _discountPercentage;

        public ObservableCollection<Menu> Menus
        {
            get => _menus;
            set => SetProperty(ref _menus, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        public ObservableCollection<MenuProduct> SelectedMenuProducts
        {
            get => _selectedMenuProducts;
            set => SetProperty(ref _selectedMenuProducts, value);
        }

        public Menu SelectedMenu
        {
            get => _selectedMenu;
            set
            {
                if (SetProperty(ref _selectedMenu, value) && value != null)
                {
                    EditingMenu = new Menu
                    {
                        MenuId = value.MenuId,
                        Name = value.Name,
                        Description = value.Description,
                        CategoryId = value.CategoryId,
                        Category = value.Category
                    };

                    SelectedMenuProducts = new ObservableCollection<MenuProduct>(
                        value.MenuProducts?.Select(mp => new MenuProduct
                        {
                            MenuId = mp.MenuId,
                            ProductId = mp.ProductId,
                            Product = mp.Product,
                            Quantity = mp.Quantity
                        }) ?? new List<MenuProduct>());

                    CalculateTotalPrice();
                    FilterProductsByCategory();
                }
            }
        }
        public string MenuDescription
        {
            get => EditingMenu?.Description;
            set
            {
                if (EditingMenu != null)
                {
                    EditingMenu.Description = value;
                    OnPropertyChanged();
                }
            }
        }
        public Menu EditingMenu
        {
            get => _editingMenu;
            set => SetProperty(ref _editingMenu, value);
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterProductsByCategory();
                }
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsAddingNew
        {
            get => _isAddingNew;
            set => SetProperty(ref _isAddingNew, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set => SetProperty(ref _discountPercentage, value);
        }

        public decimal TotalPrice { get; private set; }
        public decimal DiscountedPrice { get; private set; }

        public ICommand BackToDashboardCommand { get; }
        public ICommand AddNewMenuCommand { get; }
        public ICommand EditMenuCommand { get; }
        public ICommand DeleteMenuCommand { get; }
        public ICommand SaveMenuCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand AddProductToMenuCommand { get; }
        public ICommand RemoveProductFromMenuCommand { get; }

        public MenuManagementViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;
            _configuration = _dataService.GetConfiguration();

            DiscountPercentage = _configuration.MenuDiscountPercentage;

            Menus = new ObservableCollection<Menu>();
            Categories = new ObservableCollection<Category>();
            Products = new ObservableCollection<Product>();
            FilteredProducts = new ObservableCollection<Product>();
            SelectedMenuProducts = new ObservableCollection<MenuProduct>();

            EditingMenu = new Menu();

            BackToDashboardCommand = new RelayCommand(() => _navigationService.NavigateTo<AdminDashboardViewModel>());
            AddNewMenuCommand = new RelayCommand(StartAddNewMenu);
            EditMenuCommand = new RelayCommand(StartEditMenu, CanEditMenu);
            DeleteMenuCommand = new AsyncRelayCommand(DeleteMenuAsync, CanEditMenu);
            SaveMenuCommand = new AsyncRelayCommand(SaveMenuAsync, CanSaveMenu);
            CancelEditCommand = new RelayCommand(CancelEdit);
            AddProductToMenuCommand = new RelayCommand<Product>(AddProductToMenu);
            RemoveProductFromMenuCommand = new RelayCommand<MenuProduct>(RemoveProductFromMenu);

            Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var menus = await _dataService.GetMenusAsync();
                var categories = await _dataService.GetCategoriesAsync();
                var products = await _dataService.GetProductsAsync();

                Menus = new ObservableCollection<Menu>(menus);
                Categories = new ObservableCollection<Category>(categories);
                Products = new ObservableCollection<Product>(products);

                var allCategory = new Category
                {
                    CategoryId = 0,
                    Name = "Toate produsele"
                };
                Categories.Insert(0, allCategory);

                FilteredProducts = new ObservableCollection<Product>(products);

                SelectedCategory = allCategory;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la încărcarea datelor: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterProductsByCategory()
        {
            if (SelectedCategory == null || Products == null)
                return;

            IEnumerable<Product> productsInCategory;
            if (SelectedCategory.CategoryId == 0)
            {
                productsInCategory = Products;
            }
            else
            {
                productsInCategory = Products.Where(p => p.CategoryId == SelectedCategory.CategoryId);
            }

            if (SelectedMenuProducts != null && SelectedMenuProducts.Count > 0)
            {
                var selectedProductIds = SelectedMenuProducts.Select(mp => mp.ProductId).ToList();
                productsInCategory = productsInCategory.Where(p => !selectedProductIds.Contains(p.ProductId));
            }

            FilteredProducts = new ObservableCollection<Product>(productsInCategory);
        }

        private void StartAddNewMenu()
        {
            EditingMenu = new Menu
            {
                Description = ""
            };

            SelectedMenuProducts = new ObservableCollection<MenuProduct>();
            IsAddingNew = true;
            IsEditing = true;
            CalculateTotalPrice();
            FilterProductsByCategory();
        }

        private void StartEditMenu()
        {
            if (SelectedMenu == null)
                return;

            IsAddingNew = false;
            IsEditing = true;
        }

        private bool CanEditMenu()
        {
            return SelectedMenu != null && !IsEditing;
        }

        private bool CanSaveMenu()
        {
            return IsEditing && EditingMenu != null && !string.IsNullOrWhiteSpace(EditingMenu.Name)
                && EditingMenu.CategoryId > 0 && SelectedMenuProducts.Count > 0;
        }

        private void CancelEdit()
        {
            IsEditing = false;
            IsAddingNew = false;

            if (!IsAddingNew && SelectedMenu != null)
            {
                EditingMenu = new Menu
                {
                    MenuId = SelectedMenu.MenuId,
                    Name = SelectedMenu.Name,
                    Description = SelectedMenu.Description,
                    CategoryId = SelectedMenu.CategoryId,
                    Category = SelectedMenu.Category
                };

                SelectedMenuProducts = new ObservableCollection<MenuProduct>(
                    SelectedMenu.MenuProducts?.Select(mp => new MenuProduct
                    {
                        MenuId = mp.MenuId,
                        ProductId = mp.ProductId,
                        Product = mp.Product,
                        Quantity = mp.Quantity
                    }) ?? new List<MenuProduct>());
            }
            else
            {
                EditingMenu = new Menu
                {
                    Description = ""
                };
                SelectedMenuProducts = new ObservableCollection<MenuProduct>();
            }

            CalculateTotalPrice();
            FilterProductsByCategory();
        }

        private async Task SaveMenuAsync()
        {
            if (!CanSaveMenu())
                return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                if (IsAddingNew)
                {
                    int newMenuId = await _dataService.AddMenuAsync(EditingMenu, SelectedMenuProducts.ToList());
                    EditingMenu.MenuId = newMenuId;
                }
                else
                {
                    await _dataService.UpdateMenuAsync(EditingMenu, SelectedMenuProducts.ToList());
                }

                await LoadDataAsync();

                IsEditing = false;
                IsAddingNew = false;

                SelectedMenu = Menus.FirstOrDefault(m => m.MenuId == EditingMenu.MenuId);
            }
            catch (Exception ex)
            {
                var fullErrorMessage = ex.Message;
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    fullErrorMessage += " -> " + innerException.Message;
                    innerException = innerException.InnerException;
                }

                ErrorMessage = $"Eroare la salvarea meniului: {fullErrorMessage}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteMenuAsync()
        {
            if (SelectedMenu == null)
                return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                await _dataService.DeleteMenuAsync(SelectedMenu.MenuId);
                Menus.Remove(SelectedMenu);
                SelectedMenu = null;
                EditingMenu = new Menu();
                SelectedMenuProducts = new ObservableCollection<MenuProduct>();
                FilterProductsByCategory();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la ștergerea meniului: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddProductToMenu(Product product)
        {
            if (product == null || !IsEditing)
                return;

            var existingProduct = SelectedMenuProducts.FirstOrDefault(mp => mp.ProductId == product.ProductId);

            if (existingProduct != null)
            {
                existingProduct.Quantity += 1;
            }
            else
            {
                SelectedMenuProducts.Add(new MenuProduct
                {
                    MenuId = EditingMenu.MenuId,
                    ProductId = product.ProductId,
                    Product = product,
                    Quantity = 1
                });
                FilterProductsByCategory();
            }

            CalculateTotalPrice();
        }

        private void RemoveProductFromMenu(MenuProduct menuProduct)
        {
            if (menuProduct == null || !IsEditing)
                return;

            SelectedMenuProducts.Remove(menuProduct);
            CalculateTotalPrice();
            FilterProductsByCategory();
        }

        private void CalculateTotalPrice()
        {
            if (SelectedMenuProducts == null || SelectedMenuProducts.Count == 0)
            {
                TotalPrice = 0;
                DiscountedPrice = 0;
                return;
            }

            TotalPrice = SelectedMenuProducts.Sum(mp => mp.Product.Price * mp.Quantity);
            DiscountedPrice = TotalPrice * (1 - (DiscountPercentage / 100));

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(DiscountedPrice));
        }
    }
}
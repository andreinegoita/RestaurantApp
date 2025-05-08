using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace RestaurantApp.ViewModels
{
    public class ProductManagementViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Product> _products;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Allergen> _allergens;
        private ObservableCollection<Allergen> _selectedAllergens;
        private ObservableCollection<ProductImage> _productImages;

        private Product _selectedProduct;
        private Product _newProduct;
        private Category _selectedCategory;
        private bool _isLoading;
        private bool _isEditing;
        private bool _isAddingNew;
        private string _searchKeyword;
        private bool _includeAllergen;
        private bool _excludeAllergen;
        private bool _isEditingOrNoSelection;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<Allergen> Allergens
        {
            get => _allergens;
            set => SetProperty(ref _allergens, value);
        }

        public ObservableCollection<Allergen> SelectedAllergens
        {
            get => _selectedAllergens;
            set => SetProperty(ref _selectedAllergens, value);
        }

        public ObservableCollection<ProductImage> ProductImages
        {
            get => _productImages;
            set => SetProperty(ref _productImages, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value) && value != null)
                {
                    Task.Run(() => LoadProductDetailsAsync());
                    UpdateIsEditingOrNoSelection();
                    OnPropertyChanged(nameof(IsEditingOrNoSelection));
                }
                else if (SetProperty(ref _selectedProduct, value))
                {
                    UpdateIsEditingOrNoSelection();
                    OnPropertyChanged(nameof(IsEditingOrNoSelection));
                }
            }
        }


        public bool IsEditingOrNoSelection
        {
            get => _isEditingOrNoSelection;
            private set => SetProperty(ref _isEditingOrNoSelection, value);
        }

        private void UpdateIsEditingOrNoSelection()
        {
            IsEditingOrNoSelection = IsEditing || SelectedProduct == null;
        }


        public Product NewProduct
        {
            get => _newProduct;
            set => SetProperty(ref _newProduct, value);
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    UpdateIsEditingOrNoSelection();
                    OnPropertyChanged(nameof(IsEditingOrNoSelection));
                }
            }
        }

        public bool IsAddingNew
        {
            get => _isAddingNew;
            set => SetProperty(ref _isAddingNew, value);
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
            set => SetProperty(ref _excludeAllergen, value);
        }

        public ICommand BackCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddImageCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand ToggleAllergenCommand { get; }

        public ProductManagementViewModel(INavigationService navigationService, IDataService dataService, IDialogService dialogService)
        {
            _navigationService = navigationService;
            _dataService = dataService;
            _dialogService = dialogService;
            IsEditingOrNoSelection = IsEditing || SelectedProduct == null;
            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();
            Allergens = new ObservableCollection<Allergen>();
            SelectedAllergens = new ObservableCollection<Allergen>();
            ProductImages = new ObservableCollection<ProductImage>();

            NewProduct = new Product
            {
                TotalQuantity = 0,
                PortionQuantity = 1,
                Unit = "g"
            };

            BackCommand = new RelayCommand(() => _navigationService.NavigateTo<AdminDashboardViewModel>());
            AddNewCommand = new RelayCommand(StartAddNew);
            EditCommand = new RelayCommand(StartEdit, CanEdit);
            SaveCommand = new AsyncRelayCommand(SaveProductAsync, CanSave);
            CancelCommand = new RelayCommand(CancelEdit);
            DeleteCommand = new AsyncRelayCommand(DeleteProductAsync, CanDelete);
            SearchCommand = new AsyncRelayCommand(SearchProductsAsync);
            AddImageCommand = new AsyncRelayCommand(AddProductImageAsync);
            RemoveImageCommand = new AsyncRelayCommand<ProductImage>(RemoveProductImageAsync);
            ToggleAllergenCommand = new RelayCommand<Allergen>(ToggleAllergen);

            Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                await LoadAllergensAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to load data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async Task LoadProductsAsync()
        {
            var products = await _dataService.GetProductsAsync();
            Products = new ObservableCollection<Product>(products);
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _dataService.GetCategoriesAsync();
            Categories = new ObservableCollection<Category>(categories);
        }

        private async Task LoadAllergensAsync()
        {
            var allergens = await _dataService.GetAllergensAsync();
            Allergens = new ObservableCollection<Allergen>(allergens);
        }

        private async Task LoadProductDetailsAsync()
        {
            if (SelectedProduct == null)
                return;

            try
            {
                IsLoading = true;

                var productWithDetails = await _dataService.GetProductWithDetailsAsync(SelectedProduct.ProductId);

                if (productWithDetails != null)
                {
                    SelectedProduct = productWithDetails;

                    ProductImages = new ObservableCollection<ProductImage>(
                        productWithDetails.ProductImages ?? new List<ProductImage>());

                    SelectedAllergens = new ObservableCollection<Allergen>(
                        productWithDetails.ProductAllergens?.Select(pa => pa.Allergen) ?? new List<Allergen>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product details: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to load product details: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private void StartAddNew()
        {
            IsAddingNew = true;
            IsEditing = true;
            SelectedProduct = null;
            NewProduct = new Product
            {
                TotalQuantity = 0,
                PortionQuantity = 1,
                Unit = "g"
            };
            SelectedAllergens.Clear();
            ProductImages.Clear();
        }

        private void StartEdit()
        {
            if (SelectedProduct == null)
                return;

            IsEditing = true;
            IsAddingNew = false;
            NewProduct = new Product
            {
                ProductId = SelectedProduct.ProductId,
                Name = SelectedProduct.Name,
                Price = SelectedProduct.Price,
                PortionQuantity = SelectedProduct.PortionQuantity,
                Unit = SelectedProduct.Unit,
                TotalQuantity = SelectedProduct.TotalQuantity,
                CategoryId = SelectedProduct.CategoryId
            };
            SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == SelectedProduct.CategoryId);
        }

        private bool CanEdit()
        {
            return SelectedProduct != null && !IsEditing;
        }

        private bool CanSave()
        {
            return IsEditing && NewProduct != null && !string.IsNullOrWhiteSpace(NewProduct.Name) &&
                   NewProduct.Price > 0 && NewProduct.PortionQuantity > 0 &&
                   !string.IsNullOrWhiteSpace(NewProduct.Unit) && SelectedCategory != null;
        }

        private bool CanDelete()
        {
            return SelectedProduct != null && !IsEditing;
        }

        private void CancelEdit()
        {
            IsEditing = false;
            IsAddingNew = false;
        }

        private async Task SaveProductAsync()
        {
            if (NewProduct == null || SelectedCategory == null)
                return;

            try
            {
                IsLoading = true;
                NewProduct.CategoryId = SelectedCategory.CategoryId;

                if (IsAddingNew)
                {
                    int productId = await _dataService.AddProductAsync(NewProduct);
                    if (productId > 0)
                    {
                        await SaveProductImagesAsync(productId);
                        await SaveProductAllergensAsync(productId);
                        await _dialogService.ShowMessageAsync("Success", "Product added successfully.");
                    }
                }
                else
                {
                    await _dataService.UpdateProductAsync(NewProduct);
                    await SaveProductImagesAsync(NewProduct.ProductId);
                    await SaveProductAllergensAsync(NewProduct.ProductId);
                    await _dialogService.ShowMessageAsync("Success", "Product updated successfully.");
                }

                IsEditing = false;
                IsAddingNew = false;
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving product: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to save product: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveProductImagesAsync(int productId)
        {
            if (ProductImages == null || !ProductImages.Any())
                return;

            foreach (var image in ProductImages)
            {
                image.ProductId = productId;
            }

            await _dataService.UpdateProductImagesAsync(productId, ProductImages.ToList());
        }

        private async Task SaveProductAllergensAsync(int productId)
        {
            if (SelectedAllergens == null)
                return;

            var allergenIds = SelectedAllergens.Select(a => a.AllergenId).ToList();
            await _dataService.UpdateProductAllergensAsync(productId, allergenIds);
        }

        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null)
                return;

            var result = await _dialogService.ShowConfirmationAsync(
                "Delete Product",
                $"Are you sure you want to delete {SelectedProduct.Name}?");

            if (!result)
                return;

            try
            {
                IsLoading = true;
                await _dataService.DeleteProductAsync(SelectedProduct.ProductId);
                await LoadProductsAsync();
                await _dialogService.ShowMessageAsync("Success", "Product deleted successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting product: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to delete product: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchProductsAsync()
        {
            try
            {
                IsLoading = true;
                var products = await _dataService.SearchProductsAsync(SearchKeyword, IncludeAllergen, ExcludeAllergen);
                Products = new ObservableCollection<Product>(products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching products: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to search products: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddProductImageAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string fileName = Path.GetFileName(dialog.FileName);
                string targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Products");

                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                string targetPath = Path.Combine(targetDirectory, fileName);

                if (File.Exists(targetPath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string uniqueFileName = $"{fileNameWithoutExt}_{DateTime.Now.Ticks}{extension}";
                    targetPath = Path.Combine(targetDirectory, uniqueFileName);
                    fileName = uniqueFileName;
                }

                File.Copy(dialog.FileName, targetPath);

                string relativePath = $"/Images/Products/{fileName}";

                ProductImages.Add(new ProductImage
                {
                    ImageUrl = relativePath
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding image: {ex.Message}");
                await _dialogService.ShowMessageAsync("Error", $"Failed to add image: {ex.Message}");
            }
        }

        private async Task RemoveProductImageAsync(ProductImage image)
        {
            if (image == null)
                return;

            var result = await _dialogService.ShowConfirmationAsync(
                "Remove Image",
                "Are you sure you want to remove this image?");

            if (!result)
                return;

            ProductImages.Remove(image);
        }

        private void ToggleAllergen(Allergen allergen)
        {
            if (allergen == null)
                return;

            if (SelectedAllergens.Any(a => a.AllergenId == allergen.AllergenId))
            {
                var existingAllergen = SelectedAllergens.First(a => a.AllergenId == allergen.AllergenId);
                SelectedAllergens.Remove(existingAllergen);
            }
            else
            {
                SelectedAllergens.Add(allergen);
            }
        }
    }
}
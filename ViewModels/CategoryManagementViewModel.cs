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
    public class CategoryManagementViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;

        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;
        private string _categoryName;
        private string _categoryDescription;
        private bool _isEditing;
        private bool _isLoading;
        private string _errorMessage;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && value != null)
                {
                    CategoryName = value.Name;
                    CategoryDescription = value.Description;
                    IsEditing = true;
                }
            }
        }

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        public string CategoryDescription
        {
            get => _categoryDescription;
            set => SetProperty(ref _categoryDescription, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
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

        public ICommand LoadCategoriesCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand UpdateCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand BackToAdminDashboardCommand { get; }

        public CategoryManagementViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            Categories = new ObservableCollection<Category>();

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, CanAddOrUpdateCategory);
            UpdateCategoryCommand = new AsyncRelayCommand(UpdateCategoryAsync, CanAddOrUpdateCategory);
            DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, () => SelectedCategory != null);
            ClearFormCommand = new RelayCommand(ClearForm);
            BackToAdminDashboardCommand = new RelayCommand(BackToAdminDashboard);

            Task.Run(() => LoadCategoriesAsync());
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categories = await _dataService.GetCategoriesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories = new ObservableCollection<Category>(categories);
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la încărcarea categoriilor: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddCategoryAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var newCategory = new Category
                {
                    Name = CategoryName,
                    Description = CategoryDescription
                };

                int categoryId = await _dataService.AddCategoryAsync(newCategory);
                newCategory.CategoryId = categoryId;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Add(newCategory);
                });

                ClearForm();
                MessageBox.Show("Categoria a fost adăugată cu succes!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la adăugarea categoriei: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error adding category: {ex.Message}");
                MessageBox.Show($"Eroare la adăugarea categoriei: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateCategoryAsync()
        {
            if (SelectedCategory == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                SelectedCategory.Name = CategoryName;
                SelectedCategory.Description = CategoryDescription;

                await _dataService.UpdateCategoryAsync(SelectedCategory);

                await LoadCategoriesAsync();
                ClearForm();
                MessageBox.Show("Categoria a fost actualizată cu succes!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la actualizarea categoriei: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error updating category: {ex.Message}");
                MessageBox.Show($"Eroare la actualizarea categoriei: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null)
                return;

            var result = MessageBox.Show(
                $"Ești sigur că vrei să ștergi categoria '{SelectedCategory.Name}'? Această acțiune va șterge toate produsele și meniurile asociate cu această categorie.",
                "Confirmare ștergere",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _dataService.DeleteCategoryAsync(SelectedCategory.CategoryId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Remove(SelectedCategory);
                });

                ClearForm();
                MessageBox.Show("Categoria a fost ștearsă cu succes!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la ștergerea categoriei: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error deleting category: {ex.Message}");
                MessageBox.Show($"Eroare la ștergerea categoriei: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanAddOrUpdateCategory()
        {
            return !string.IsNullOrWhiteSpace(CategoryName);
        }

        private void ClearForm()
        {
            SelectedCategory = null;
            CategoryName = string.Empty;
            CategoryDescription = string.Empty;
            IsEditing = false;
        }

        private void BackToAdminDashboard()
        {
            _navigationService.NavigateTo<AdminDashboardViewModel>();
        }
    }
}
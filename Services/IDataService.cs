using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Services
{
    public interface IDataService
    {
        AppConfiguration GetConfiguration();
        Task<List<Category>> GetCategoriesAsync();
        Task<Category> GetCategoryByIdAsync(int categoryId);
        Task<int> AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int categoryId);
        Task<List<Product>> GetProductsAsync();
        Task<Product> GetProductByIdAsync(int productId);
        Task<List<Product>> SearchProductsAsync(string keyword, bool includeAllergen, bool excludeAllergen);
        Task<int> AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int productId);
        Task<List<Product>> GetLowStockProductsAsync();
        Task UpdateProductImagesAsync(int productId, List<ProductImage> images);
        Task UpdateProductAllergensAsync(int productId, List<int> allergenIds);
        Task<Product> GetProductWithDetailsAsync(int productId);
        Task<List<Menu>> GetMenusAsync();
        Task<Menu> GetMenuByIdAsync(int menuId);
        Task<int> AddMenuAsync(Menu menu, List<MenuProduct> menuProducts);
        Task UpdateMenuAsync(Menu menu, List<MenuProduct> menuProducts);
        Task DeleteMenuAsync(int menuId);
        Task<List<Allergen>> GetAllergensAsync();
        Task<Allergen> GetAllergenByIdAsync(int allergenId);
        Task<int> AddAllergenAsync(Allergen allergen);
        Task UpdateAllergenAsync(Allergen allergen);
        Task DeleteAllergenAsync(int allergenId);
        Task<User> AuthenticateUserAsync(string email, string passwordHash);
        Task<int> RegisterUserAsync(User user);
        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetActiveOrdersAsync();
        Task<List<Order>> GetUserOrdersAsync(int userId);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<int> CreateOrderAsync(Order order, List<OrderItem> orderItems);
        Task UpdateOrderStatusAsync(int orderId, int statusId);
    }
}

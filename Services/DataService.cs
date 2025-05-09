using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using RestaurantApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Services
{
    public class DataService : IDataService
    {
        private readonly RestaurantDbContext _dbContext;
        private readonly string _connectionString;
        private readonly AppConfiguration _configuration;

        public DataService(RestaurantDbContext dbContext)
        {
            _dbContext = dbContext;
            _connectionString = ConfigurationManager.ConnectionStrings["RestaurantDbConnection"].ConnectionString;

            _configuration = new AppConfiguration
            {
                MenuDiscountPercentage = decimal.Parse(ConfigurationManager.AppSettings["MenuDiscountPercent"] ?? "10"),
                MinOrderValueForDiscount = decimal.Parse(ConfigurationManager.AppSettings["OrderDiscountThreshold"] ?? "200"),
                OrderDiscountPercentage = decimal.Parse(ConfigurationManager.AppSettings["OrderDiscountPercent"] ?? "15"),
                MinOrderCountForDiscount = int.Parse(ConfigurationManager.AppSettings["MinOrdersForDiscount"] ?? "5"),
                DiscountTimeIntervalDays = int.Parse(ConfigurationManager.AppSettings["OrdersDiscountTimeframeInDays"] ?? "30"),
                MinOrderValueForFreeShipping = decimal.Parse(ConfigurationManager.AppSettings["FreeDeliveryThreshold"] ?? "100"),
                ShippingCost = decimal.Parse(ConfigurationManager.AppSettings["DeliveryFee"] ?? "15"),
                LowStockThreshold = double.Parse(ConfigurationManager.AppSettings["LowStockThreshold"] ?? "10")
            };
        }

        public AppConfiguration GetConfiguration()
        {
            return _configuration;
        }

        #region Categories
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _dbContext.Categories
                .Include(c => c.Products)
                .Include(c => c.Menus)
                .ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int categoryId)
        {
            return await _dbContext.Categories
                .Include(c => c.Products)
                .Include(c => c.Menus)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<int> AddCategoryAsync(Category category)
        {
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();
            return category.CategoryId;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _dbContext.Entry(category).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DeleteCategory";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

        #region Products
        public async Task<List<Product>> GetProductsAsync()
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
                .ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<List<Product>> SearchProductsAsync(string keyword, bool includeAllergen, bool excludeAllergen)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SearchProducts";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Keyword", keyword ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IncludeAllergen", includeAllergen);
                    command.Parameters.AddWithValue("@ExcludeAllergen", excludeAllergen);

                    var products = new List<Product>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                PortionQuantity = reader.GetDecimal(reader.GetOrdinal("PortionQuantity")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                TotalQuantity = reader.GetDecimal(reader.GetOrdinal("TotalQuantity")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId"))
                            });
                        }
                    }

                    foreach (var product in products)
                    {
                        await LoadProductRelatedDataAsync(product);
                    }

                    return products;
                }
            }
        }
        public async Task<Product> GetProductWithDetailsAsync(int productId)
        {
            // Exemplu de implementare cu Entity Framework:
            return await _dbContext.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAllergens)
                    .ThenInclude(pa => pa.Allergen)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }
        private async Task LoadProductRelatedDataAsync(Product product)
        {
            product.Category = await _dbContext.Categories.FindAsync(product.CategoryId);

            product.ProductImages = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == product.ProductId)
                .ToListAsync();

            product.ProductAllergens = await _dbContext.ProductAllergens
                .Where(pa => pa.ProductId == product.ProductId)
                .Include(pa => pa.Allergen)
                .ToListAsync();
        }

        public async Task<int> AddProductAsync(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "AddProduct";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@PortionQuantity", product.PortionQuantity);
                    command.Parameters.AddWithValue("@Unit", product.Unit);
                    command.Parameters.AddWithValue("@TotalQuantity", product.TotalQuantity);
                    command.Parameters.AddWithValue("@CategoryId", product.CategoryId);

                    var productIdParam = new SqlParameter
                    {
                        ParameterName = "@ProductId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(productIdParam);

                    await command.ExecuteNonQueryAsync();
                    return (int)productIdParam.Value;
                }
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UpdateProduct";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ProductId", product.ProductId);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@PortionQuantity", product.PortionQuantity);
                    command.Parameters.AddWithValue("@Unit", product.Unit);
                    command.Parameters.AddWithValue("@TotalQuantity", product.TotalQuantity);
                    command.Parameters.AddWithValue("@CategoryId", product.CategoryId);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteProductAsync(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DeleteProduct";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ProductId", productId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "GetLowStockProducts";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Threshold", _configuration.LowStockThreshold);

                    var products = new List<Product>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                TotalQuantity = reader.GetDecimal(reader.GetOrdinal("TotalQuantity")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit"))
                            });
                        }
                    }
                    return products;
                }
            }
        }

        public async Task UpdateProductImagesAsync(int productId, List<ProductImage> images)
        {
            var existingImages = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            _dbContext.ProductImages.RemoveRange(existingImages);

            foreach (var image in images)
            {
                _dbContext.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = image.ImageUrl
                });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateProductAllergensAsync(int productId, List<int> allergenIds)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingAllergens = await _dbContext.ProductAllergens
                        .Where(pa => pa.ProductId == productId)
                        .ToListAsync();

                    _dbContext.ProductAllergens.RemoveRange(existingAllergens);

                    foreach (var allergenId in allergenIds)
                    {
                        _dbContext.ProductAllergens.Add(new ProductAllergen
                        {
                            ProductId = productId,
                            AllergenId = allergenId
                        });
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        #endregion

        #region Menus
        public async Task<List<Menu>> GetMenusAsync()
        {
            return await _dbContext.Menus
                .Include(m => m.Category)
                .Include(m => m.MenuProducts)
                .ThenInclude(mp => mp.Product)
                .ThenInclude(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
                .Include(m => m.MenuProducts)
                .ThenInclude(mp => mp.Product)
                .ThenInclude(p => p.ProductImages)
                .ToListAsync();
        }

        public async Task<Menu> GetMenuByIdAsync(int menuId)
        {
            return await _dbContext.Menus
                .Include(m => m.Category)
                .Include(m => m.MenuProducts)
                .ThenInclude(mp => mp.Product)
                .ThenInclude(p => p.ProductAllergens)
                .ThenInclude(pa => pa.Allergen)
                .Include(m => m.MenuProducts)
                .ThenInclude(mp => mp.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.MenuId == menuId);
        }

        public async Task<int> AddMenuAsync(Menu menu, List<MenuProduct> menuProducts)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    _dbContext.Menus.Add(menu);
                    await _dbContext.SaveChangesAsync();

                    foreach (var menuProduct in menuProducts)
                    {
                        menuProduct.MenuId = menu.MenuId;
                        _dbContext.MenuProducts.Add(menuProduct);
                    }
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return menu.MenuId;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task UpdateMenuAsync(Menu menu, List<MenuProduct> menuProducts)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingMenu = await _dbContext.Menus.FindAsync(menu.MenuId);
                    if (existingMenu != null)
                    {
                        _dbContext.Entry(existingMenu).State = EntityState.Detached;
                    }

                    _dbContext.Entry(menu).State = EntityState.Modified;

                    var existingMenuProducts = await _dbContext.MenuProducts
                        .Where(mp => mp.MenuId == menu.MenuId)
                        .ToListAsync();
                    _dbContext.MenuProducts.RemoveRange(existingMenuProducts);

                    foreach (var menuProduct in menuProducts)
                    {
                        menuProduct.MenuId = menu.MenuId; 
                        _dbContext.MenuProducts.Add(menuProduct);
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task DeleteMenuAsync(int menuId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DeleteMenu";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@MenuId", menuId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

        #region Allergens
        public async Task<List<Allergen>> GetAllergensAsync()
        {
            return await _dbContext.Allergens.ToListAsync();
        }

        public async Task<Allergen> GetAllergenByIdAsync(int allergenId)
        {
            return await _dbContext.Allergens.FindAsync(allergenId);
        }

        public async Task<int> AddAllergenAsync(Allergen allergen)
        {
            _dbContext.Allergens.Add(allergen);
            await _dbContext.SaveChangesAsync();
            return allergen.AllergenId;
        }

        public async Task UpdateAllergenAsync(Allergen allergen)
        {
            _dbContext.Entry(allergen).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAllergenAsync(int allergenId)
        {
            var allergen = await _dbContext.Allergens.FindAsync(allergenId);
            if (allergen != null)
            {
                _dbContext.Allergens.Remove(allergen);
                await _dbContext.SaveChangesAsync();
            }
        }
        #endregion

        #region User Authentication
        public async Task<User> AuthenticateUserAsync(string email, string passwordHash)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "AuthenticateUser";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                DeliveryAddress = reader.GetString(reader.GetOrdinal("DeliveryAddress")),
                                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<int> RegisterUserAsync(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "RegisterUser";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                    command.Parameters.AddWithValue("@DeliveryAddress", user.DeliveryAddress);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@RoleId", user.RoleId);

                    var userIdParam = new SqlParameter
                    {
                        ParameterName = "@UserId",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userIdParam);

                    await command.ExecuteNonQueryAsync();
                    return (int)userIdParam.Value;
                }
            }
        }
        #endregion

        #region Orders
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Menu)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetActiveOrdersAsync()
        {
            return await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Menu)
                .Where(o => o.StatusId != 4 && o.StatusId != 5) 
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _dbContext.Orders
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Menu)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Menu)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<int> CreateOrderAsync(Order order, List<OrderItem> orderItems)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                bool qualifiesForDiscount = await CheckDiscountEligibilityAsync(order.UserId);

                decimal subtotal = orderItems.Sum(item => item.UnitPrice * item.Quantity);
                decimal deliveryFee = subtotal >= _configuration.MinOrderValueForFreeShipping ? 0 : _configuration.ShippingCost;

                decimal discount = 0;
                if (subtotal >= _configuration.MinOrderValueForDiscount || qualifiesForDiscount)
                {
                    discount = subtotal * (_configuration.OrderDiscountPercentage / 100);
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int orderId;
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = "CreateOrder";
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@OrderCode", order.OrderCode);
                            command.Parameters.AddWithValue("@OrderDate", order.OrderDate);
                            command.Parameters.AddWithValue("@EstimatedDeliveryTime", DateTime.Now.AddHours(1));
                            command.Parameters.AddWithValue("@DeliveryFee", deliveryFee);
                            command.Parameters.AddWithValue("@DiscountAmount", discount);
                            command.Parameters.AddWithValue("@UserId", order.UserId);
                            command.Parameters.AddWithValue("@StatusId", 1); 

                            var orderIdParam = new SqlParameter
                            {
                                ParameterName = "@OrderId",
                                SqlDbType = SqlDbType.Int,
                                Direction = ParameterDirection.Output
                            };
                            command.Parameters.Add(orderIdParam);

                            await command.ExecuteNonQueryAsync();
                            orderId = (int)orderIdParam.Value;
                        }

                        foreach (var item in orderItems)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "AddOrderItem";
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@MenuId", item.MenuId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return orderId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task<bool> CheckDiscountEligibilityAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CheckDiscountEligibility";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@MinOrderCount", _configuration.MinOrderCountForDiscount);
                    command.Parameters.AddWithValue("@TimeIntervalDays", _configuration.DiscountTimeIntervalDays);

                    var eligibleParam = new SqlParameter
                    {
                        ParameterName = "@IsEligible",
                        SqlDbType = SqlDbType.Bit,
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(eligibleParam);

                    await command.ExecuteNonQueryAsync();
                    return (bool)eligibleParam.Value;
                }
            }
        }

        public async Task UpdateOrderStatusAsync(int orderId, int statusId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UpdateOrderStatus";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@StatusId", statusId);

                    await command.ExecuteNonQueryAsync();

                    if (statusId == 2) 
                    {
                        await UpdateProductQuantitiesAsync(orderId);
                    }
                }
            }
        }

        private async Task UpdateProductQuantitiesAsync(int orderId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UpdateProductQuantitiesForOrder";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion
    }
}

using BCrypt.Net;
using Restaurant.Data;
using RestaurantApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Data
{
    public static class DbInitializer
    {


        public static void Initialize(RestaurantDbContext context)
        {
            context.Database.EnsureCreated();

            CreateStoredProcedures(context);

        }

        private static void CreateStoredProcedures(RestaurantDbContext context)
        {
            var storedProcedures = new List<string>
            {
                @"
                CREATE OR ALTER PROCEDURE DeleteCategory
                    @CategoryId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- Șterge toate produsele din categoria respectivă
                        DELETE FROM Products WHERE CategoryId = @CategoryId;
                        
                        -- Șterge toate meniurile din categoria respectivă
                        DELETE FROM Menus WHERE CategoryId = @CategoryId;
                        
                        -- Șterge categoria
                        DELETE FROM Categories WHERE CategoryId = @CategoryId;
                        
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE AddProduct
                    @Name NVARCHAR(200),
                    @Price DECIMAL(18, 2),
                    @PortionQuantity DECIMAL(18, 2),
                    @Unit NVARCHAR(10),
                    @TotalQuantity DECIMAL(18, 2),
                    @CategoryId INT,
                    @ProductId INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO Products(Name, Price, PortionQuantity, Unit, TotalQuantity, CategoryId)
                    VALUES(@Name, @Price, @PortionQuantity, @Unit, @TotalQuantity, @CategoryId);
                    
                    SET @ProductId = SCOPE_IDENTITY();
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE UpdateProduct
                    @ProductId INT,
                    @Name NVARCHAR(200),
                    @Price DECIMAL(18, 2),
                    @PortionQuantity DECIMAL(18, 2),
                    @Unit NVARCHAR(10),
                    @TotalQuantity DECIMAL(18, 2),
                    @CategoryId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    UPDATE Products
                    SET Name = @Name,
                        Price = @Price,
                        PortionQuantity = @PortionQuantity,
                        Unit = @Unit,
                        TotalQuantity = @TotalQuantity,
                        CategoryId = @CategoryId
                    WHERE ProductId = @ProductId;
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE DeleteProduct
                    @ProductId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- Șterge toate legăturile produsului cu alergenii
                        DELETE FROM ProductAllergens WHERE ProductId = @ProductId;
                        
                        -- Șterge toate imaginile produsului
                        DELETE FROM ProductImages WHERE ProductId = @ProductId;
                        
                        -- Șterge legăturile produsului cu meniuri
                        DELETE FROM MenuProducts WHERE ProductId = @ProductId;
                        
                        -- Șterge produsul
                        DELETE FROM Products WHERE ProductId = @ProductId;
                        
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE DeleteMenu
                    @MenuId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- Șterge toate legăturile meniului cu produse
                        DELETE FROM MenuProducts WHERE MenuId = @MenuId;
                        
                        -- Șterge meniul
                        DELETE FROM Menus WHERE MenuId = @MenuId;
                        
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE SearchProducts
                    @Keyword NVARCHAR(MAX) = NULL,
                    @IncludeAllergen BIT = 0,
                    @ExcludeAllergen BIT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    IF @IncludeAllergen = 1 AND @ExcludeAllergen = 0
                    BEGIN
                        -- Caută produse care conțin un alergen specific
                        SELECT DISTINCT p.*
                        FROM Products p
                        INNER JOIN ProductAllergens pa ON p.ProductId = pa.ProductId
                        INNER JOIN Allergens a ON pa.AllergenId = a.AllergenId
                        WHERE (@Keyword IS NULL OR a.Name LIKE '%' + @Keyword + '%')
                        ORDER BY p.CategoryId, p.Name;
                    END
                    ELSE IF @ExcludeAllergen = 1 AND @IncludeAllergen = 0
                    BEGIN
                        -- Caută produse care NU conțin un alergen specific
                        SELECT p.*
                        FROM Products p
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM ProductAllergens pa 
                            JOIN Allergens a ON pa.AllergenId = a.AllergenId
                            WHERE pa.ProductId = p.ProductId 
                            AND (@Keyword IS NULL OR a.Name LIKE '%' + @Keyword + '%')
                        )
                        ORDER BY p.CategoryId, p.Name;
                    END
                    ELSE
                    BEGIN
                        -- Caută produse după denumire
                        SELECT p.*
                        FROM Products p
                        WHERE (@Keyword IS NULL OR p.Name LIKE '%' + @Keyword + '%')
                        ORDER BY p.CategoryId, p.Name;
                    END
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE AuthenticateUser
                    @Email NVARCHAR(255),
                    @PasswordHash NVARCHAR(255)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    SELECT u.UserId, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.DeliveryAddress, u.RoleId
                    FROM Users u
                    WHERE u.Email = @Email AND u.PasswordHash = @PasswordHash;
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE RegisterUser
                    @FirstName NVARCHAR(100),
                    @LastName NVARCHAR(100),
                    @Email NVARCHAR(255),
                    @PhoneNumber NVARCHAR(20),
                    @DeliveryAddress NVARCHAR(500),
                    @PasswordHash NVARCHAR(255),
                    @RoleId INT,
                    @UserId INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    -- Verifică dacă există deja un utilizator cu acest email
                    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
                    BEGIN
                        THROW 50000, 'Există deja un utilizator cu acest email.', 1;
                        RETURN;
                    END
                    
                    INSERT INTO Users(FirstName, LastName, Email, PhoneNumber, DeliveryAddress, PasswordHash, RoleId)
                    VALUES(@FirstName, @LastName, @Email, @PhoneNumber, @DeliveryAddress, @PasswordHash, @RoleId);
                    
                    SET @UserId = SCOPE_IDENTITY();
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE CreateOrder
                    @OrderCode NVARCHAR(50),
                    @OrderDate DATETIME,
                    @EstimatedDeliveryTime DATETIME,
                    @DeliveryFee DECIMAL(18, 2),
                    @DiscountAmount DECIMAL(18, 2),
                    @UserId INT,
                    @StatusId INT,
                    @OrderId INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO Orders(OrderCode, OrderDate, EstimatedDeliveryTime, DeliveryFee, DiscountAmount, UserId, StatusId)
                    VALUES(@OrderCode, @OrderDate, @EstimatedDeliveryTime, @DeliveryFee, @DiscountAmount, @UserId, @StatusId);
                    
                    SET @OrderId = SCOPE_IDENTITY();
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE AddOrderItem
                    @OrderId INT,
                    @ProductId INT = NULL,
                    @MenuId INT = NULL,
                    @Quantity INT,
                    @UnitPrice DECIMAL(18, 2)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO OrderItems(OrderId, ProductId, MenuId, Quantity, UnitPrice)
                    VALUES(@OrderId, @ProductId, @MenuId, @Quantity, @UnitPrice);
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE UpdateOrderStatus
                    @OrderId INT,
                    @StatusId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    UPDATE Orders
                    SET StatusId = @StatusId
                    WHERE OrderId = @OrderId;
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE UpdateProductQuantitiesForOrder
                    @OrderId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- Actualizează cantitățile produselor individuale din comandă
                        UPDATE p
                        SET p.TotalQuantity = p.TotalQuantity - (oi.Quantity * p.PortionQuantity)
                        FROM Products p
                        JOIN OrderItems oi ON p.ProductId = oi.ProductId
                        WHERE oi.OrderId = @OrderId AND oi.ProductId IS NOT NULL;
                        
                        -- Pentru produsele din meniuri
                        UPDATE p
                        SET p.TotalQuantity = p.TotalQuantity - (oi.Quantity * mp.Quantity)
                        FROM Products p
                        JOIN MenuProducts mp ON p.ProductId = mp.ProductId
                        JOIN OrderItems oi ON mp.MenuId = oi.MenuId
                        WHERE oi.OrderId = @OrderId AND oi.MenuId IS NOT NULL;
                        
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE CheckDiscountEligibility
                    @UserId INT,
                    @MinOrderCount INT,
                    @TimeIntervalDays INT,
                    @IsEligible BIT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    DECLARE @OrderCount INT;
                    
                    SELECT @OrderCount = COUNT(*)
                    FROM Orders
                    WHERE UserId = @UserId
                    AND OrderDate >= DATEADD(DAY, -@TimeIntervalDays, GETDATE())
                    AND StatusId = 4; -- Comenzi livrate
                    
                    IF @OrderCount >= @MinOrderCount
                        SET @IsEligible = 1;
                    ELSE
                        SET @IsEligible = 0;
                END
                ",

                @"
                CREATE OR ALTER PROCEDURE GetLowStockProducts
                    @Threshold FLOAT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    SELECT ProductId, Name, TotalQuantity, Unit
                    FROM Products
                    WHERE TotalQuantity <= @Threshold
                    ORDER BY TotalQuantity ASC;
                END
                "
            };

            foreach (var procedure in storedProcedures)
            {
                context.Database.ExecuteSqlRaw(procedure);
            }
        }

    }      
}

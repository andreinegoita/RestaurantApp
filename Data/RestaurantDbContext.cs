using System;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Models;

namespace Restaurant.Data
{
    public class RestaurantDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuProduct> MenuProducts { get; set; }
        public DbSet<Allergen> Allergens { get; set; }
        public DbSet<ProductAllergen> ProductAllergens { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }

        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<ProductAllergen>()
                .HasKey(pa => new { pa.ProductId, pa.AllergenId });

            modelBuilder.Entity<ProductAllergen>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAllergens)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductAllergen>()
                .HasOne(pa => pa.Allergen)
                .WithMany(a => a.ProductAllergens)
                .HasForeignKey(pa => pa.AllergenId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuProduct>()
                .HasKey(mp => new { mp.MenuId, mp.ProductId });

            modelBuilder.Entity<MenuProduct>()
                .HasOne(mp => mp.Menu)
                .WithMany(m => m.MenuProducts)
                .HasForeignKey(mp => mp.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuProduct>()
                .HasOne(mp => mp.Product)
                .WithMany(p => p.MenuProducts)
                .HasForeignKey(mp => mp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Menu)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(oi => oi.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<OrderStatus>().HasData(
                new OrderStatus { StatusId = 1, Name = "Înregistrată" },
                new OrderStatus { StatusId = 2, Name = "Se pregătește" },
                new OrderStatus { StatusId = 3, Name = "A plecat la client" },
                new OrderStatus { StatusId = 4, Name = "Livrată" },
                new OrderStatus { StatusId = 5, Name = "Anulată" }
            );

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { RoleId = 1, Name = "Client" },
                new UserRole { RoleId = 2, Name = "Angajat" }
            );


            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
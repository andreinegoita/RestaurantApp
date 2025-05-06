using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PortionQuantity { get; set; }

        [Required]
        [StringLength(10)]
        public string Unit { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalQuantity { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ProductAllergen> ProductAllergens { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<MenuProduct> MenuProducts { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        public Product()
        {
            ProductAllergens = new HashSet<ProductAllergen>();
            ProductImages = new HashSet<ProductImage>();
            MenuProducts = new HashSet<MenuProduct>();
            OrderItems = new HashSet<OrderItem>();
        }
    }

}

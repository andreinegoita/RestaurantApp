using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Menu
    {
        [Key]
        public int MenuId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<MenuProduct> MenuProducts { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        public Menu()
        {
            MenuProducts = new HashSet<MenuProduct>();
            OrderItems = new HashSet<OrderItem>();
        }

        [NotMapped]
        public decimal Price
        {
            get
            {
                decimal totalPrice = 0;
                foreach (var item in MenuProducts)
                {
                    totalPrice += item.Product.Price * item.Quantity / item.Product.PortionQuantity;
                }

               
                decimal discountPercent = 10;
                return totalPrice * (1 - discountPercent / 100);
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class MenuProduct
    {
        [Key, Column(Order = 0)]
        public int MenuId { get; set; }

        [Key, Column(Order = 1)]
        public int ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }

}

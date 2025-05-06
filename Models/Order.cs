using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public DateTime? EstimatedDeliveryTime { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DeliveryFee { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DiscountAmount { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int StatusId { get; set; }

        [ForeignKey("StatusId")]
        public virtual OrderStatus Status { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }

        public Order()
        {
            OrderItems = new HashSet<OrderItem>();
            OrderDate = DateTime.Now;
            OrderCode = "ORD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }

        [NotMapped]
        public decimal SubTotal
        {
            get
            {
                decimal total = 0;
                foreach (var item in OrderItems)
                {
                    total += item.TotalPrice;
                }
                return total;
            }
        }

        [NotMapped]
        public decimal TotalPrice
        {
            get
            {
                return SubTotal + DeliveryFee - DiscountAmount;
            }
        }
    }

}

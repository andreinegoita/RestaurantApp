using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp
{
    public class AppConfiguration
    {
        public decimal MenuDiscountPercentage { get; set; } 
        public decimal MinOrderValueForDiscount { get; set; } 
        public int MinOrderCountForDiscount { get; set; } 
        public int DiscountTimeIntervalDays { get; set; } 
        public decimal OrderDiscountPercentage { get; set; }
        public decimal MinOrderValueForFreeShipping { get; set; } 
        public decimal ShippingCost { get; set; } 
        public double LowStockThreshold { get; set; } 
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.Models
{
    public class Allergen
    {
        [Key]
        public int AllergenId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public virtual ICollection<ProductAllergen> ProductAllergens { get; set; }

        public Allergen()
        {
            ProductAllergens = new HashSet<ProductAllergen>();
        }
    }
}

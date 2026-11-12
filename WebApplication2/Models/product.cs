using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("product")]
    public class product
    {
        [Key]
        [Display(Name = "ID")]
        public int prdid {  get; set; }
        [Required(ErrorMessage = "Product name is required.")]
        [MaxLength(20)]
        [Display(Name = "Product Name")]
        public string? prdname { get; set; }
        [MaxLength(40)]
        [Display(Name = "Description")]
        public string? prddescription { get; set; }
        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 999999999.99, ErrorMessage = "Price must be 0 or greater.")]
        [Display(Name = "Price")]
        public decimal price { get; set; } = 0.0m;

    }
}

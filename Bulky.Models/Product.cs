using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }    
        public string Description { get; set; }
        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }

        [DisplayName("List Price")]
        [Range(1,1000)]
        [Required]
        public double ListPrice { get; set; }  
        
        [Range(1,1000)]
        [Required]
        [DisplayName("Price for 1-50")]
        public double Price { get; set; }  
        
        [Range(1,1000)]
        [Required]
        [DisplayName("Price for 50+")]
        public double Price50 { get; set; }  
        
        [Range(1,1000)]
        [Required]
        [DisplayName("Price for 100+")]
        public double Price100 { get; set; }

        
        public int CategoryId { get; set; } //For the Foreign Key it is need a Property relation with Category. Look down!

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

        [ValidateNever]
        public string ImageUrl { get; set; }
    }
}

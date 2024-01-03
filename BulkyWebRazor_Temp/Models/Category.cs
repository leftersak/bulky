using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BulkyWebRazor_Temp.Models
{
    public class Category
    {
        [Key] //If the name is purely Id, EF Core will AUTOMATICALLY think that this is the primary key, so DataAnnotations is not necessary
        public int Id { get; set; }
        [Required]
        [DisplayName("Category Name")]
        [MaxLength(30)]
        public string Name { get; set; }
        [DisplayName("Display Order")]//How the property will be displayed in the UI with asp-for
        [Range(1, 100, ErrorMessage = "Displa Order must be between 1-100")]
        public int DisplayOrder { get; set; }
    }
}

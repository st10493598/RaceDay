using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage =
            "Category name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = "";

        [StringLength(255, ErrorMessage =
            "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int EventId { get; set; }
    }
}
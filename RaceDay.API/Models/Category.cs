
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        // Navigation Property
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    /// <summary>
    /// Represents a category within a RaceDay event (e.g., "Men 18-39", "Women Open").
    /// Each category belongs to exactly one event.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Unique identifier for the category (Primary Key).
        /// </summary>
        [Key]
        public int CategoryID { get; set; }

        /// <summary>
        /// Foreign key linking this category to its parent Event.
        /// </summary>
        [Required]
        public int EventID { get; set; }

        /// <summary>
        /// Name of the category (e.g., "Men 18-39", "Women Open").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the category.
        /// </summary>
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Navigation property linking back to the parent Event.
        /// </summary>
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }
    }
}

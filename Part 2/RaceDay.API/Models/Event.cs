using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    /// <summary>
    /// Represents a RaceDay event (running, walking, or cycling).
    /// Each event is created and managed by a single Organiser.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Unique identifier for the event (Primary Key).
        /// </summary>
        [Key]
        public int EventID { get; set; }

        /// <summary>
        /// Foreign key linking this event to the Organiser who created it.
        /// </summary>
        [Required]
        public int OrganiserID { get; set; }

        /// <summary>
        /// The name of the event (e.g., "Cape Town Marathon 2026").
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the event.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The date and time the event takes place.
        /// </summary>
        [Required]
        public DateTime EventDate { get; set; }

        /// <summary>
        /// The location where the event is held.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// The distance of the event in kilometres.
        /// </summary>
        [Required]
        [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Distance { get; set; }

        /// <summary>
        /// Type of event. Must be "Running", "Walking", or "Cycling".
        /// </summary>
        [Required]
        [MaxLength(50)]
        [RegularExpression("^(Running|Walking|Cycling)$")]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// The UTC date and time when this event was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property linking back to the owning Organiser.
        /// </summary>
        [ForeignKey(nameof(OrganiserID))]
        public Organiser? Organiser { get; set; }
    }
}

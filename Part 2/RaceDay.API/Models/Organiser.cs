using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    /// <summary>
    /// Represents an Organiser profile. Each Organiser is linked one-to-one with a User account.
    /// Organisers can create and manage RaceDay events.
    /// </summary>
    public class Organiser
    {
        /// <summary>
        /// Unique identifier for the organiser profile (Primary Key).
        /// </summary>
        [Key]
        public int OrganiserID { get; set; }

        /// <summary>
        /// Foreign key linking this profile to a User account. Must be unique (one-to-one).
        /// </summary>
        [Required]
        public int UserID { get; set; }

        /// <summary>
        /// Optional name of the organisation this organiser represents.
        /// </summary>
        [MaxLength(255)]
        public string? OrganisationName { get; set; }

        /// <summary>
        /// Navigation property linking back to the owning User account.
        /// </summary>
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    /// <summary>
    /// Represents a user account in the RaceDay system.
    /// A user can be either an Organiser or a Participant.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user (Primary Key).
        /// </summary>
        [Key]
        public int UserID { get; set; }

        /// <summary>
        /// User's email address. Must be unique across all users.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt-hashed password. Never stored in plain text.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Role of the user. Must be either "Organiser" or "Participant".
        /// </summary>
        [Required]
        [MaxLength(20)]
        [RegularExpression("^(Organiser|Participant)$")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The UTC date and time when this account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

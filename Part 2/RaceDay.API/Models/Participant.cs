using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    /// <summary>
    /// Represents a Participant profile. Each Participant is linked one-to-one with a User account.
    /// Participants can enrol in events and track their race results.
    /// </summary>
    public class Participant
    {
        /// <summary>
        /// Unique identifier for the participant profile (Primary Key).
        /// </summary>
        [Key]
        public int ParticipantID { get; set; }

        /// <summary>
        /// Foreign key linking this profile to a User account. Must be unique (one-to-one).
        /// </summary>
        [Required]
        public int UserID { get; set; }

        /// <summary>
        /// Optional date of birth of the participant.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Optional gender of the participant.
        /// </summary>
        [MaxLength(20)]
        public string? Gender { get; set; }

        /// <summary>
        /// Optional contact number for the participant.
        /// </summary>
        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        /// <summary>
        /// Navigation property linking back to the owning User account.
        /// </summary>
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
    }
}

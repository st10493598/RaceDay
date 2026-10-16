using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ProfileUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Optional participant fields
        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }

        [Required]
        public int UserID { get; set; }

        [MaxLength(255)]
        public string? OrganisationName { get; set; }

        // Navigation Property
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
    }
}

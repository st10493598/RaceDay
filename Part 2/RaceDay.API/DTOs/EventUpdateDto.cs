using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EventUpdateDto
    {
        [Required]
        [MaxLength(255)]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public decimal Distance { get; set; }

        [Required]
        [RegularExpression("^(Running|Walking|Cycling)$")]
        public string EventType { get; set; } = string.Empty;
    }
}
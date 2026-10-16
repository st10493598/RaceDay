using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ResultUpdateDto
    {
        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required]
        [RegularExpression("^(DNS|DNF|Finished)$")]
        public string Status { get; set; } = "Finished";
    }
}

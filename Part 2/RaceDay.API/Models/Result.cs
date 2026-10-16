
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression("^(DNS|DNF|Finished)$")]
        public string Status { get; set; } = "DNS";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey(nameof(EnrolmentID))]
        public Enrolment? Enrolment { get; set; }
    }
}


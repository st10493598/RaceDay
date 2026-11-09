
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        [RegularExpression("^(Pending|Confirmed|Cancelled)$")]
        public string Status { get; set; } = "Pending";

        // Navigation Properties
        [ForeignKey(nameof(ParticipantID))]
        public Participant? Participant { get; set; }

        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }

        [ForeignKey(nameof(CategoryID))]
        public Category? Category { get; set; }
    }
}

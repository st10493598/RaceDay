using System;

namespace RaceDayMVC.Models
{
    public class ParticipantResultViewModel
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        public string? Status { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? EventName { get; set; }

        public string? CategoryName { get; set; }

        public DateTime? EventDate { get; set; }
    }
}
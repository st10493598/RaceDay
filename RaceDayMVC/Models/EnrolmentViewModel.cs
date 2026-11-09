using System;

namespace RaceDayMVC.Models
{
    public class EnrolmentViewModel
    {
        public int EnrolmentId { get; set; }

        public string Status { get; set; } = "";

        public DateTime EnrolmentDate { get; set; }

        public int ParticipantId { get; set; }

        public string? ParticipantFirstName { get; set; }

        public string? ParticipantLastName { get; set; }

        public string? ParticipantEmail { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int EventId { get; set; }

        public string? EventName { get; set; }

        public DateTime? EventDate { get; set; }

        public string ParticipantFullName
        {
            get
            {
                return $"{ParticipantFirstName} {ParticipantLastName}".Trim();
            }
        }
    }
}

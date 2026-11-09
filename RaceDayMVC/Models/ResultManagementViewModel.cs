using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ResultManagementViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = "";

        public List<RaceResultViewModel> Results { get; set; }
            = new List<RaceResultViewModel>();
    }

    public class RaceResultViewModel
    {
        public int ResultID { get; set; }

        public int EnrolmentID { get; set; }

        public string? ParticipantFirstName { get; set; }

        public string? ParticipantLastName { get; set; }

        public string ParticipantName =>
            $"{ParticipantFirstName} {ParticipantLastName}".Trim();

        public string? CategoryName { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        public string Status { get; set; } = "";
    }

    public class ResultEditViewModel : IValidatableObject
    {
        public int ResultID { get; set; }

        public int EventId { get; set; }

        public string ParticipantName { get; set; } = "";

        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Position must be greater than zero.")]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required]
        [RegularExpression("^(DNS|DNF|Finished)$",
            ErrorMessage = "Invalid race status.")]
        public string Status { get; set; } = "Finished";

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Status == "Finished")
            {
                if (!FinishTime.HasValue)
                {
                    yield return new ValidationResult(
                        "Finish Time is required.",
                        new[] { nameof(FinishTime) });
                }

                if (!ChipTime.HasValue)
                {
                    yield return new ValidationResult(
                        "Chip Time is required.",
                        new[] { nameof(ChipTime) });
                }

                if (!FinishPosition.HasValue)
                {
                    yield return new ValidationResult(
                        "Finishing Position is required.",
                        new[] { nameof(FinishPosition) });
                }
            }
        }
    }
}
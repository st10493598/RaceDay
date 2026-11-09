
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ResultCaptureViewModel : IValidatableObject
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage =
            "Please select a participant enrolment.")]
        [Range(1, int.MaxValue,
            ErrorMessage =
            "Please select a participant enrolment.")]
        public int EnrolmentId { get; set; }

        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage =
            "Finishing position must be greater than zero.")]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required(ErrorMessage =
            "Please select a result status.")]
        [RegularExpression("^(DNS|DNF|Finished)$",
            ErrorMessage = "Invalid result status.")]
        public string Status { get; set; } = "Finished";

        public List<EnrolmentViewModel> Enrolments { get; set; }
            = new List<EnrolmentViewModel>();

        // Conditional validation for completed races
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Status == "Finished")
            {
                if (!FinishTime.HasValue)
                {
                    yield return new ValidationResult(
                        "Finish Time is required for a finished race.",
                        new[] { nameof(FinishTime) });
                }

                if (!ChipTime.HasValue)
                {
                    yield return new ValidationResult(
                        "Chip Time is required for a finished race.",
                        new[] { nameof(ChipTime) });
                }

                if (!FinishPosition.HasValue)
                {
                    yield return new ValidationResult(
                        "Finishing Position is required for a finished race.",
                        new[] { nameof(FinishPosition) });
                }
            }
        }
    }
}

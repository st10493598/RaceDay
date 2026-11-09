using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ResultUpdateDto : IValidatableObject
    {
        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Finishing position must be greater than zero.")]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required(ErrorMessage = "Result status is required.")]
        [RegularExpression("^(DNS|DNF|Finished)$",
            ErrorMessage = "Status must be DNS, DNF or Finished.")]
        public string Status { get; set; } = "Finished";

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
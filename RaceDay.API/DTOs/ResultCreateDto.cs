
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ResultCreateDto : IValidatableObject
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? FinishPosition { get; set; }

        public TimeSpan? ChipTime { get; set; }

        [Required]
        [RegularExpression("^(DNS|DNF|Finished)$")]
        public string Status { get; set; } = "Finished";

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

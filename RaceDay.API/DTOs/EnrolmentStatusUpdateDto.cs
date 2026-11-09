using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EnrolmentStatusUpdateDto
    {
        [Required(ErrorMessage = "Enrolment status is required.")]
        [RegularExpression(
            "^(Pending|Confirmed|Cancelled)$",
            ErrorMessage = "Status must be Pending, Confirmed or Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}

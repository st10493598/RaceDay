using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ParticipantRegistrationViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a race category.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a race category.")]
        public int CategoryId { get; set; }

        public List<ParticipantCategoryViewModel> Categories { get; set; }
            = new List<ParticipantCategoryViewModel>();
    }

    public class ParticipantCategoryViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int EventId { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = "";

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; } = "";

        [Required(ErrorMessage = "Distance is required.")]
        [Range(0.01, 10000)]
        [Display(Name = "Distance (km)")]
        public decimal Distance { get; set; }

        [Required(ErrorMessage = "Event type is required.")]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "";

        public int OrganiserId { get; set; }

        public string? OrganiserName { get; set; }

        // ============================================
        // STEP 32: EVENT BANNER IMAGE
        // ============================================

        [Display(Name = "Event Banner")]
        public string? BannerImageUrl { get; set; }
    }
}
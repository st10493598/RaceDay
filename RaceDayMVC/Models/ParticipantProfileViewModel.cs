using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ParticipantProfileViewModel
    {
        public int UserId { get; set; }

        public string? Email { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        public string? Role { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}
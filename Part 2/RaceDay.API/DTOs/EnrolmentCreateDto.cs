using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EnrolmentCreateDto
    {
        [Required]
        public int CategoryID { get; set; }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to view your profile."
                });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Role == "Organiser")
            {
                var organiser = await _context.Organisers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.UserID == userId.Value);

                return Ok(new
                {
                    userId = user.UserID,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role,
                    organisationName = organiser?.OrganisationName
                });
            }
            else if (user.Role == "Participant")
            {
                var participant = await _context.Participants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserID == userId.Value);

                return Ok(new
                {
                    userId = user.UserID,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role,
                    dateOfBirth = participant?.DateOfBirth,
                    gender = participant?.Gender,
                    contactNumber = participant?.ContactNumber
                });
            }

            return BadRequest(new
            {
                message = "Invalid user role."
            });
        }

        // PUT: api/users/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] ProfileUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to update your profile."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;

            if (user.Role == "Participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.UserID == userId.Value);

                if (participant != null)
                {
                    participant.ContactNumber = dto.ContactNumber;
                    participant.DateOfBirth = dto.DateOfBirth;
                    participant.Gender = dto.Gender;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully."
            });
        }
    }
}

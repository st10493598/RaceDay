using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public UsersController(
            ApplicationDbContext context,
            BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
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
                    contactNumber = participant?.ContactNumber,
                    profilePictureUrl = participant?.ProfilePictureUrl
                });
            }

            return BadRequest(new { message = "Invalid user role." });
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

        // POST: api/users/profile/picture
        [HttpPost("profile/picture")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024 + 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile image)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to upload a profile picture."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Role != "Participant")
                return StatusCode(403, new
                {
                    message = "Only participants can upload profile pictures."
                });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserID == userId.Value);

            if (participant == null)
                return NotFound(new
                {
                    message = "Participant profile not found."
                });

            if (image == null || image.Length == 0)
                return BadRequest(new
                {
                    message = "Please select an image."
                });

            try
            {
                var imageUrl =
                    await _blobStorageService.UploadImageAsync(image);

                participant.ProfilePictureUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile picture uploaded successfully.",
                    profilePictureUrl = imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
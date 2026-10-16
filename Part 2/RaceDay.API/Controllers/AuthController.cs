using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles user authentication and registration for the RaceDay system.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers a new user (Organiser or Participant) with their personal details.
        /// </summary>
        /// <remarks>
        /// The password is hashed using BCrypt before storage. A role-specific profile is created automatically.
        /// </remarks>
        /// <param name="dto">Registration details including email, password, name, and role.</param>
        /// <returns>201 Created with the new user's ID and role, or an error status.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (existingUser != null)
                return Conflict(new { message = "A user with this email already exists." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (dto.Role == "Organiser")
            {
                var organiser = new Organiser
                {
                    UserID = user.UserID,
                    OrganisationName = dto.OrganisationName
                };
                _context.Organisers.Add(organiser);
            }
            else if (dto.Role == "Participant")
            {
                var participant = new Participant
                {
                    UserID = user.UserID
                };
                _context.Participants.Add(participant);
            }

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Registration successful.",
                userId = user.UserID,
                email = user.Email,
                role = user.Role
            });
        }

        /// <summary>
        /// Authenticates a registered user and creates a session for them.
        /// </summary>
        /// <remarks>
        /// On successful login, the user's ID, Role, Email, and FirstName are stored in the session.
        /// </remarks>
        /// <param name="dto">Login credentials (email and password).</param>
        /// <returns>200 OK with user details, or 401 Unauthorized if credentials are invalid.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("FirstName", user.FirstName);

            return Ok(new
            {
                message = "Login successful.",
                userId = user.UserID,
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }

        /// <summary>
        /// Logs the current user out by clearing their session.
        /// </summary>
        /// <returns>200 OK confirming the logout.</returns>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }
    }
}

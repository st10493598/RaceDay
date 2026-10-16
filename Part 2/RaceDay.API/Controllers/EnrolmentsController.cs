using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/events/{eventId}/enrolments
        [HttpPost("events/{eventId}/enrolments")]
        public async Task<IActionResult> CreateEnrolment(int eventId, [FromBody] EnrolmentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Participant")
                return StatusCode(403, new { message = "Only Participants can enrol in events." });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserID == userId.Value);

            if (participant == null)
                return NotFound(new { message = "Participant profile not found." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.EventID != eventId)
                return BadRequest(new { message = "This category does not belong to the selected event." });

            var existing = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.ParticipantID == participant.ParticipantID
                                       && e.EventID == eventId);

            if (existing != null)
                return Conflict(new { message = "You are already enrolled in this event." });

            var enrolment = new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                EventID = eventId,
                CategoryID = dto.CategoryID,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Enrolment successful.",
                enrolmentId = enrolment.EnrolmentID,
                eventId = enrolment.EventID,
                categoryId = enrolment.CategoryID
            });
        }

        // GET: api/enrolments
        [HttpGet("enrolments")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Participant")
                return StatusCode(403, new { message = "Only Participants can view their enrolments." });

            var participant = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserID == userId.Value);

            if (participant == null)
                return NotFound(new { message = "Participant profile not found." });

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantID == participant.ParticipantID)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Select(e => new
                {
                    enrolmentId = e.EnrolmentID,
                    status = e.Status,
                    enrolmentDate = e.EnrolmentDate,
                    eventId = e.EventID,
                    eventName = e.Event != null ? e.Event.EventName : null,
                    eventDate = e.Event != null ? e.Event.EventDate : (DateTime?)null,
                    categoryId = e.CategoryID,
                    categoryName = e.Category != null ? e.Category.CategoryName : null
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET: api/events/{eventId}/enrolments
        [HttpGet("events/{eventId}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can view event enrolments." });

            var organiser = await _context.Organisers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var evt = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only view enrolments for your own events." });

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.EventID == eventId)
                .Include(e => e.Participant)
                    .ThenInclude(p => p!.User)
                .Include(e => e.Category)
                .Select(e => new
                {
                    enrolmentId = e.EnrolmentID,
                    status = e.Status,
                    enrolmentDate = e.EnrolmentDate,
                    participantId = e.ParticipantID,
                    participantFirstName = e.Participant != null && e.Participant.User != null
                        ? e.Participant.User.FirstName : null,
                    participantLastName = e.Participant != null && e.Participant.User != null
                        ? e.Participant.User.LastName : null,
                    participantEmail = e.Participant != null && e.Participant.User != null
                        ? e.Participant.User.Email : null,
                    categoryId = e.CategoryID,
                    categoryName = e.Category != null ? e.Category.CategoryName : null
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // DELETE: api/enrolments/{id}
        [HttpDelete("enrolments/{id}")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Participant")
                return StatusCode(403, new { message = "Only Participants can cancel enrolments." });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserID == userId.Value);

            if (participant == null)
                return NotFound(new { message = "Participant profile not found." });

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.ParticipantID != participant.ParticipantID)
                return StatusCode(403, new { message = "You can only cancel your own enrolments." });

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

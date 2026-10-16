using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages event enrolments. Participants can enrol in events and view their own enrolments.
    /// Organisers can view enrolments for their own events.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initialises the controller with the application database context.
        /// </summary>
        /// <param name="context">The database context used to access enrolment data.</param>
        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Enrols the logged-in Participant in a specific event by selecting a category.
        /// </summary>
        /// <param name="eventId">The ID of the event to enrol in.</param>
        /// <param name="dto">The selected category ID.</param>
        /// <returns>
        /// 201 Created if successful, 400 Bad Request for invalid data,
        /// 401 Unauthorized if not logged in, 403 Forbidden for an incorrect role,
        /// 404 Not Found if the participant, event, or category does not exist,
        /// or 409 Conflict if already enrolled.
        /// </returns>
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

        /// <summary>
        /// Returns the enrolment history of the currently logged-in Participant.
        /// </summary>
        /// <returns>
        /// 200 OK with a list of the participant's enrolments,
        /// 401 Unauthorized if not logged in, 403 Forbidden for an incorrect role,
        /// or 404 Not Found if the participant profile does not exist.
        /// </returns>
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

        /// <summary>
        /// Returns all enrolments for a specific event. Only the owning Organiser can view this.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>
        /// 200 OK with a list of enrolled participants,
        /// 401 Unauthorized if not logged in, 403 Forbidden if the user is not
        /// the event's owning Organiser, or 404 Not Found if the organiser profile
        /// or event does not exist.
        /// </returns>
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

        /// <summary>
        /// Cancels an enrolment. Participants can only cancel their own enrolments.
        /// </summary>
        /// <param name="id">The enrolment ID to cancel.</param>
        /// <returns>
        /// 204 No Content on success, 401 Unauthorized if not logged in,
        /// 403 Forbidden if attempting to cancel another participant's enrolment,
        /// or 404 Not Found if the participant profile or enrolment does not exist.
        /// </returns>
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

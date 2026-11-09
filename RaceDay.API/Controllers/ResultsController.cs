using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/events/{eventId}/results
        // Organiser only - captures a result for a participant's enrolment
        [HttpPost("events/{eventId}/results")]
        public async Task<IActionResult> CreateResult(int eventId, [FromBody] ResultCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can capture results." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            // Verify the event exists and belongs to this organiser
            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only capture results for your own events." });

            // Verify the enrolment exists and belongs to this event
            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentID);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.EventID != eventId)
                return BadRequest(new { message = "This enrolment does not belong to the selected event." });

            // Check if a result already exists for this enrolment
            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.EnrolmentID == dto.EnrolmentID);

            if (existingResult != null)
                return Conflict(new { message = "A result already exists for this enrolment. Use PUT to update it." });

            var result = new Result
            {
                EnrolmentID = dto.EnrolmentID,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition,
                ChipTime = dto.ChipTime,
                Status = dto.Status,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Result created successfully.",
                resultId = result.ResultID,
                enrolmentId = result.EnrolmentID,
                status = result.Status
            });
        }

        // PUT: api/results/{id}
        // Organiser only - updates a result for their own event
        [HttpPut("results/{id}")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can update results." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            // Verify the organiser owns the event linked to this result
            if (result.Enrolment == null || result.Enrolment.Event == null
                || result.Enrolment.Event.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only update results for your own events." });

            result.FinishTime = dto.FinishTime;
            result.FinishPosition = dto.FinishPosition;
            result.ChipTime = dto.ChipTime;
            result.Status = dto.Status;
            result.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Result updated successfully." });
        }

        // GET: api/events/{eventId}/results
        // Any authenticated user - leaderboard view
        [HttpGet("events/{eventId}/results")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                        .ThenInclude(p => p!.User)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment != null && r.Enrolment.EventID == eventId)
                .OrderBy(r => r.FinishPosition)
                .Select(r => new
                {
                    resultId = r.ResultID,
                    enrolmentId = r.EnrolmentID,
                    finishTime = r.FinishTime,
                    finishPosition = r.FinishPosition,
                    chipTime = r.ChipTime,
                    status = r.Status,
                    participantFirstName = r.Enrolment!.Participant != null
                        && r.Enrolment.Participant.User != null
                            ? r.Enrolment.Participant.User.FirstName : null,
                    participantLastName = r.Enrolment.Participant != null
                        && r.Enrolment.Participant.User != null
                            ? r.Enrolment.Participant.User.LastName : null,
                    categoryName = r.Enrolment.Category != null
                        ? r.Enrolment.Category.CategoryName : null
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/enrolments/{enrolmentId}/result
        // Participant only - view their own result for a specific enrolment
        [HttpGet("enrolments/{enrolmentId}/result")]
        public async Task<IActionResult> GetMyResult(int enrolmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Participant")
                return StatusCode(403, new { message = "Only Participants can view their own results here." });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserID == userId.Value);

            if (participant == null)
                return NotFound(new { message = "Participant profile not found." });

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            // Verify this enrolment belongs to this participant
            if (enrolment.ParticipantID != participant.ParticipantID)
                return StatusCode(403, new { message = "You can only view your own results." });

            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.EnrolmentID == enrolmentId);

            if (result == null)
                return NotFound(new { message = "No result has been captured for this enrolment yet." });

            return Ok(new
            {
                resultId = result.ResultID,
                enrolmentId = result.EnrolmentID,
                finishTime = result.FinishTime,
                finishPosition = result.FinishPosition,
                chipTime = result.ChipTime,
                status = result.Status,
                updatedAt = result.UpdatedAt
            });
        }
    }
}


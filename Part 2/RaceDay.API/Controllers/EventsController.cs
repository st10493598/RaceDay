using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/events
        // Any authenticated user (Organiser or Participant) can view all events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "You must be logged in to view events." });

            var events = await _context.Events
                .Include(e => e.Organiser)
                .Select(e => new
                {
                    eventId = e.EventID,
                    eventName = e.EventName,
                    description = e.Description,
                    eventDate = e.EventDate,
                    location = e.Location,
                    distance = e.Distance,
                    eventType = e.EventType,
                    organiserId = e.OrganiserID,
                    organiserName = e.Organiser != null ? e.Organiser.OrganisationName : null
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/{id}
        // Any authenticated user can view a single event
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "You must be logged in to view events." });

            var evt = await _context.Events
                .Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            return Ok(new
            {
                eventId = evt.EventID,
                eventName = evt.EventName,
                description = evt.Description,
                eventDate = evt.EventDate,
                location = evt.Location,
                distance = evt.Distance,
                eventType = evt.EventType,
                organiserId = evt.OrganiserID,
                organiserName = evt.Organiser?.OrganisationName
            });
        }

        // POST: api/events
        // Organiser only - creates a new event
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can create events." });

            // Find the organiser profile linked to this user
            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var newEvent = new Event
            {
                OrganiserID = organiser.OrganiserID,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                Distance = dto.Distance,
                EventType = dto.EventType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Event created successfully.",
                eventId = newEvent.EventID,
                eventName = newEvent.EventName
            });
        }

        // PUT: api/events/{id}
        // Organiser only - can only update their own events
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can update events." });

            // Get organiser profile
            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            // Check the organiser owns this event
            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only update events you created." });

            evt.EventName = dto.EventName;
            evt.Description = dto.Description;
            evt.EventDate = dto.EventDate;
            evt.Location = dto.Location;
            evt.Distance = dto.Distance;
            evt.EventType = dto.EventType;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Event updated successfully." });
        }

        // DELETE: api/events/{id}
        // Organiser only - can only delete their own events
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can delete events." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only delete events you created." });

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

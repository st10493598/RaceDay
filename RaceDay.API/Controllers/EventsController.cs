using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public EventsController(
            ApplicationDbContext context,
            BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        // ============================================
        // GET ALL EVENTS
        // GET: api/events
        // ============================================

        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to view events."
                });

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
                    organiserName = e.Organiser != null
                        ? e.Organiser.OrganisationName
                        : null,
                    bannerImageUrl = e.BannerImageUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        // ============================================
        // GET EVENT BY ID
        // GET: api/events/{id}
        // ============================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to view events."
                });

            var evt = await _context.Events
                .Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new
                {
                    message = "Event not found."
                });

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
                organiserName = evt.Organiser?.OrganisationName,
                bannerImageUrl = evt.BannerImageUrl
            });
        }

        // ============================================
        // CREATE EVENT
        // POST: api/events
        // ============================================

        [HttpPost]
        public async Task<IActionResult> CreateEvent(
            [FromBody] EventCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can create events."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(
                    o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new
                {
                    message = "Organiser profile not found."
                });

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

        // ============================================
        // UPDATE EVENT
        // PUT: api/events/{id}
        // ============================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] EventUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can update events."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(
                    o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new
                {
                    message = "Organiser profile not found."
                });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new
                {
                    message = "Event not found."
                });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new
                {
                    message = "You can only update events you created."
                });

            evt.EventName = dto.EventName;
            evt.Description = dto.Description;
            evt.EventDate = dto.EventDate;
            evt.Location = dto.Location;
            evt.Distance = dto.Distance;
            evt.EventType = dto.EventType;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully."
            });
        }

        // ============================================
        // DELETE EVENT
        // DELETE: api/events/{id}
        // ============================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can delete events."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(
                    o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new
                {
                    message = "Organiser profile not found."
                });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new
                {
                    message = "Event not found."
                });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new
                {
                    message = "You can only delete events you created."
                });

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ============================================
        // ORGANISER DASHBOARD STATISTICS
        // GET: api/events/organiser/dashboard
        // ============================================

        [HttpGet("organiser/dashboard")]
        public async Task<IActionResult> GetOrganiserDashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can view this dashboard."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(
                    o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new
                {
                    message = "Organiser profile not found."
                });

            var events = await _context.Events
                .Where(e =>
                    e.OrganiserID == organiser.OrganiserID)
                .Select(e => new
                {
                    eventId = e.EventID,
                    eventName = e.EventName,
                    eventDate = e.EventDate,
                    location = e.Location,
                    bannerImageUrl = e.BannerImageUrl,

                    totalEnrolments = _context.Enrolments
                        .Count(en => en.EventID == e.EventID)
                })
                .OrderBy(e => e.eventDate)
                .ToListAsync();

            int totalEvents = events.Count;

            int totalEnrolments =
                events.Sum(e => e.totalEnrolments);

            int upcomingEvents = events.Count(
                e => e.eventDate.Date >= DateTime.Today);

            return Ok(new
            {
                totalEvents,
                totalEnrolments,
                upcomingEvents,
                events
            });
        }

        // ============================================
        // UPLOAD EVENT BANNER
        // POST: api/events/{id}/banner
        // ============================================

        [HttpPost("{id:int}/banner")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadEventBanner(
            int id,
            IFormFile image)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can upload event banners."
                });

            if (image == null || image.Length == 0)
                return BadRequest(new
                {
                    message = "Please select an image."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(
                    o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new
                {
                    message = "Organiser profile not found."
                });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (evt == null)
                return NotFound(new
                {
                    message = "Event not found."
                });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new
                {
                    message = "You can only upload banners for events you created."
                });

            try
            {
                string imageUrl =
                    await _blobStorageService.UploadImageAsync(image);

                evt.BannerImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Event banner uploaded successfully.",
                    eventId = evt.EventID,
                    bannerImageUrl = evt.BannerImageUrl
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
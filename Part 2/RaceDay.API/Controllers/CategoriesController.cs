using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages event categories in the RaceDay system, including age groups
    /// and gender groups.
    /// </summary>
    /// <remarks>
    /// Authenticated users can view event categories. Only Organisers can
    /// create, update, or delete categories belonging to their own events.
    /// </remarks>
    [Route("api")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initialises a new instance of the <see cref="CategoriesController"/> class.
        /// </summary>
        /// <param name="context">
        /// The database context used to access events, categories, and organisers.
        /// </param>
        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all categories belonging to a specified event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <returns>
        /// Returns HTTP 200 OK with the categories,
        /// HTTP 401 Unauthorized if the user is not logged in, or
        /// HTTP 404 Not Found if the event does not exist.
        /// </returns>
        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "You must be logged in to view categories."
                });

            var evt = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventID == eventId)
                .Select(c => new
                {
                    categoryId = c.CategoryID,
                    categoryName = c.CategoryName,
                    description = c.Description,
                    eventId = c.EventID
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Creates a new category for an event owned by the current Organiser.
        /// </summary>
        /// <param name="eventId">The identifier of the event receiving the category.</param>
        /// <param name="dto">The category name and description supplied in the request body.</param>
        /// <returns>
        /// Returns HTTP 201 Created when the category is created,
        /// HTTP 400 Bad Request for invalid input,
        /// HTTP 401 Unauthorized if the user is not logged in,
        /// HTTP 403 Forbidden if the user is not authorised, or
        /// HTTP 404 Not Found if the organiser profile or event does not exist.
        /// </returns>
        [HttpPost("events/{eventId}/categories")]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can create categories."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new
                {
                    message = "You can only add categories to your own events."
                });

            var category = new Category
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                Description = dto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Category created successfully.",
                categoryId = category.CategoryID,
                categoryName = category.CategoryName
            });
        }

        /// <summary>
        /// Updates an existing category belonging to the current Organiser's event.
        /// </summary>
        /// <param name="id">The unique identifier of the category to update.</param>
        /// <param name="dto">The updated category name and description.</param>
        /// <returns>
        /// Returns HTTP 200 OK when the category is updated,
        /// HTTP 400 Bad Request for invalid input,
        /// HTTP 401 Unauthorized if the user is not logged in,
        /// HTTP 403 Forbidden if the user is not authorised, or
        /// HTTP 404 Not Found if the organiser profile or category does not exist.
        /// </returns>
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can update categories."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != organiser.OrganiserID)
            {
                return StatusCode(403, new
                {
                    message = "You can only update categories for your own events."
                });
            }

            category.CategoryName = dto.CategoryName;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully." });
        }

        /// <summary>
        /// Deletes a category belonging to the current Organiser's event.
        /// </summary>
        /// <param name="id">The unique identifier of the category to delete.</param>
        /// <returns>
        /// Returns HTTP 204 No Content when the category is deleted,
        /// HTTP 401 Unauthorized if the user is not logged in,
        /// HTTP 403 Forbidden if the user is not authorised, or
        /// HTTP 404 Not Found if the organiser profile or category does not exist.
        /// </returns>
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new
                {
                    message = "Only Organisers can delete categories."
                });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != organiser.OrganiserID)
            {
                return StatusCode(403, new
                {
                    message = "You can only delete categories for your own events."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

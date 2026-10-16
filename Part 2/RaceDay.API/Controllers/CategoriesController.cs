using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/events/{eventId}/categories
        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "You must be logged in to view categories." });

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

        // POST: api/events/{eventId}/categories
        [HttpPost("events/{eventId}/categories")]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can create categories." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (evt == null)
                return NotFound(new { message = "Event not found." });

            if (evt.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only add categories to your own events." });

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

        // PUT: api/categories/{id}
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can update categories." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null || category.Event.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only update categories for your own events." });

            category.CategoryName = dto.CategoryName;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully." });
        }

        // DELETE: api/categories/{id}
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "You must be logged in." });

            if (role != "Organiser")
                return StatusCode(403, new { message = "Only Organisers can delete categories." });

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.UserID == userId.Value);

            if (organiser == null)
                return NotFound(new { message = "Organiser profile not found." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null || category.Event.OrganiserID != organiser.OrganiserID)
                return StatusCode(403, new { message = "You can only delete categories for your own events." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

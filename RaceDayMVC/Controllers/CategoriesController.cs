
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly HttpClient _api;

        public CategoriesController(IHttpClientFactory httpClientFactory)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        private IActionResult? CheckOrganiser()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("Role") != "Organiser")
                return Forbid();

            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("ApiSessionCookie")))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        private HttpRequestMessage CreateApiRequest(
            HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            string? cookie = HttpContext.Session
                .GetString("ApiSessionCookie");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            return request;
        }

        private IActionResult SessionExpired()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // GET: Categories/Index?eventId=7
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (eventId <= 0)
                return RedirectToAction("Index", "Events");

            ViewBag.EventId = eventId;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load categories.";
                    return View(new List<CategoryViewModel>());
                }

                var categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>();

                return View(categories ?? new List<CategoryViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";
                return View(new List<CategoryViewModel>());
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid category data received.";
                return View(new List<CategoryViewModel>());
            }
        }

        // GET: Categories/Create?eventId=7
        [HttpGet]
        public IActionResult Create(int eventId)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            return View(new CategoryViewModel
            {
                EventId = eventId
            });
        }

        // POST: Categories/Create?eventId=7
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int eventId, CategoryViewModel model)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (eventId <= 0 || model.EventId != eventId)
                return BadRequest("Invalid event ID.");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Post,
                    $"api/events/{eventId}/categories");

                request.Content = JsonContent.Create(new
                {
                    model.CategoryName,
                    model.Description
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError(
                        "",
                        "You can only create categories for your own events.");

                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError(
                        "", "The selected event was not found.");

                    return View(model);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category created successfully!";

                    return RedirectToAction("Index", new { eventId });
                }

                ModelState.AddModelError(
                    "", "Unable to create category.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API.");
            }

            return View(model);
        }

        // GET: Categories/Edit/2?eventId=7
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int eventId)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (id <= 0 || eventId <= 0)
                return BadRequest("Invalid category or event ID.");

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to retrieve category details.");

                var categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>();

                var category = categories?.FirstOrDefault(
                    c => c.CategoryId == id &&
                         c.EventId == eventId);

                if (category == null)
                    return NotFound();

                return View(category);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Cannot connect to RaceDay API.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "Invalid category information received.");
            }
        }

        // POST: Categories/Edit/2?eventId=7
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, int eventId, CategoryViewModel model)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (id <= 0 || eventId <= 0 ||
                model.CategoryId != id ||
                model.EventId != eventId)
            {
                return BadRequest("Invalid category or event ID.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Put,
                    $"api/categories/{id}");

                request.Content = JsonContent.Create(new
                {
                    model.CategoryName,
                    model.Description
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError(
                        "",
                        "You can only edit categories belonging to your own events.");

                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category updated successfully!";

                    return RedirectToAction(
                        "Index", new { eventId });
                }

                ModelState.AddModelError(
                    "",
                    "Unable to update category. Check your details.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Cannot connect to RaceDay API.");
            }

            return View(model);
        }

        // GET: Categories/Delete/2?eventId=7
        [HttpGet]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (id <= 0 || eventId <= 0)
                return BadRequest("Invalid category or event ID.");

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to retrieve category details.");

                var categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>();

                var category = categories?.FirstOrDefault(
                    c => c.CategoryId == id &&
                         c.EventId == eventId);

                if (category == null)
                    return NotFound();

                return View(category);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Cannot connect to RaceDay API.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "Invalid category information received.");
            }
        }

        // POST: Categories/DeleteConfirmed/2?eventId=7
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id, int eventId)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (id <= 0 || eventId <= 0)
                return BadRequest("Invalid category or event ID.");

            try
            {
                // Verify that the category belongs to the selected event.
                using (var checkRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories"))
                {
                    using var checkResponse =
                        await _api.SendAsync(checkRequest);

                    if (checkResponse.StatusCode ==
                        HttpStatusCode.Unauthorized)
                        return SessionExpired();

                    if (checkResponse.StatusCode ==
                        HttpStatusCode.NotFound)
                        return NotFound();

                    if (!checkResponse.IsSuccessStatusCode)
                    {
                        TempData["Error"] =
                            "Unable to verify the selected category.";

                        return RedirectToAction(
                            "Index", new { eventId });
                    }

                    var categories = await checkResponse.Content
                        .ReadFromJsonAsync<List<CategoryViewModel>>();

                    if (categories == null ||
                        !categories.Any(c =>
                            c.CategoryId == id &&
                            c.EventId == eventId))
                    {
                        return NotFound();
                    }
                }

                using var request = CreateApiRequest(
                    HttpMethod.Delete,
                    $"api/categories/{id}");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    TempData["Error"] =
                        "You can only delete categories belonging to your own events.";

                    return RedirectToAction(
                        "Index", new { eventId });
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Category deleted successfully!";

                    return RedirectToAction(
                        "Index", new { eventId });
                }

                TempData["Error"] =
                    "Unable to delete category. It may have related enrolments.";

                return RedirectToAction(
                    "Index", new { eventId });
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Cannot connect to RaceDay API.";
            }
            catch (JsonException)
            {
                TempData["Error"] =
                    "Unable to verify the category information.";
            }

            return RedirectToAction("Index", new { eventId });
        }
    }
}

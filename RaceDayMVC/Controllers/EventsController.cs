
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly HttpClient _api;

        public EventsController(IHttpClientFactory httpClientFactory)
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

        // GET: Events
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, "api/events");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load events.";
                    return View(new List<EventViewModel>());
                }

                var events = await response.Content
                    .ReadFromJsonAsync<List<EventViewModel>>();

                return View(events ?? new List<EventViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";
                return View(new List<EventViewModel>());
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid event data received.";
                return View(new List<EventViewModel>());
            }
        }

        // GET: Events/Create
        [HttpGet]
        public IActionResult Create()
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            return View(new EventViewModel
            {
                EventDate = DateTime.Today
            });
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Post, "api/events");

                request.Content = JsonContent.Create(new
                {
                    model.EventName,
                    model.Description,
                    model.EventDate,
                    model.Location,
                    model.Distance,
                    model.EventType
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Event created successfully!";

                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(
                    "", "Unable to create event. Check your details.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API.");
            }

            return View(model);
        }

        // GET: Events/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, $"api/events/{id}");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to retrieve event details.");

                var model = await response.Content
                    .ReadFromJsonAsync<EventViewModel>();

                if (model == null)
                    return NotFound();

                return View(model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Cannot connect to RaceDay API.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "Invalid event information received.");
            }
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, EventViewModel model)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            if (id != model.EventId)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Put, $"api/events/{id}");

                request.Content = JsonContent.Create(new
                {
                    model.EventName,
                    model.Description,
                    model.EventDate,
                    model.Location,
                    model.Distance,
                    model.EventType
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Event updated successfully!";

                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(
                    "", "Unable to update event. Check your details.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API.");
            }

            return View(model);
        }

        // GET: Events/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, $"api/events/{id}");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to retrieve event details.");

                var model = await response.Content
                    .ReadFromJsonAsync<EventViewModel>();

                if (model == null)
                    return NotFound();

                return View(model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Cannot connect to RaceDay API.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "Invalid event information received.");
            }
        }

        // POST: Events/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var auth = CheckOrganiser();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Delete, $"api/events/{id}");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Event deleted successfully!";

                    return RedirectToAction("Index");
                }

                TempData["Error"] =
                    "Unable to delete this event. It may have related records.";

                return RedirectToAction("Index");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Cannot connect to RaceDay API.";

                return RedirectToAction("Index");
            }
        }
    }
}




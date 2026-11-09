
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class EnrolmentsController : Controller
    {
        private readonly HttpClient _api;

        public EnrolmentsController(
            IHttpClientFactory httpClientFactory)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        // GET: /Enrolments?eventId=7
        // Organiser views enrolments for their event
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Organiser")
                return Forbid();

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            string? apiSessionCookie =
                HttpContext.Session.GetString("ApiSessionCookie");

            if (string.IsNullOrWhiteSpace(apiSessionCookie))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.EventId = eventId;

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                request.Headers.TryAddWithoutValidation(
                    "Cookie", apiSessionCookie);

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return StatusCode(
                        403,
                        "You can only view enrolments for your own events.");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("The selected event was not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load event enrolments.";
                    return View(new List<EnrolmentViewModel>());
                }

                var enrolments = await response.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>();

                return View(
                    enrolments ?? new List<EnrolmentViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Cannot connect to RaceDay API. Please try again.";

                return View(new List<EnrolmentViewModel>());
            }
            catch (JsonException)
            {
                ViewBag.Error =
                    "The API returned invalid enrolment information.";

                return View(new List<EnrolmentViewModel>());
            }
        }

        // POST: /Enrolments/UpdateStatus
        // Sends the selected status to RaceDay.API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int enrolmentId,
            int eventId,
            string status)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Organiser")
                return Forbid();

            if (eventId <= 0 || enrolmentId <= 0)
                return BadRequest("Invalid event or enrolment ID.");

            if (status != "Pending" &&
                status != "Confirmed" &&
                status != "Cancelled")
            {
                TempData["Error"] = "Invalid enrolment status.";
                return RedirectToAction("Index", new { eventId });
            }

            string? apiSessionCookie =
                HttpContext.Session.GetString("ApiSessionCookie");

            if (string.IsNullOrWhiteSpace(apiSessionCookie))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Put,
                    $"api/enrolments/{enrolmentId}/status");

                request.Headers.TryAddWithoutValidation(
                    "Cookie", apiSessionCookie);

                request.Content = JsonContent.Create(new
                {
                    Status = status
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    TempData["Error"] =
                        "You can only manage enrolments for your own events.";

                    return RedirectToAction("Index", new { eventId });
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "Enrolment not found.";
                    return RedirectToAction("Index", new { eventId });
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        "Unable to update enrolment status.";

                    return RedirectToAction("Index", new { eventId });
                }

                TempData["Success"] =
                    "Enrolment status updated successfully.";

                return RedirectToAction("Index", new { eventId });
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Cannot connect to RaceDay API. Please try again.";

                return RedirectToAction("Index", new { eventId });
            }
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly HttpClient _api;

        public OrganiserController(IHttpClientFactory httpClientFactory)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        // ============================================
        // HELPER: CHECK ORGANISER LOGIN
        // ============================================

        private IActionResult? CheckOrganiser()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Organiser")
                return Forbid();

            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("ApiSessionCookie")))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        // ============================================
        // HELPER: CREATE API REQUEST
        // ============================================

        private HttpRequestMessage CreateApiRequest(
            HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            request.Headers.TryAddWithoutValidation("Cookie", cookie);

            return request;
        }

        // ============================================
        // HELPER: HANDLE EXPIRED SESSION
        // ============================================

        private IActionResult SessionExpired()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        // ============================================
        // ORGANISER DASHBOARD
        // GET: /Organiser
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");

            var model = new OrganiserDashboardViewModel();

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get,
                    "api/events/organiser/dashboard");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    ViewBag.Error =
                        "Organiser profile not found. Please contact support.";

                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Unable to load dashboard statistics.";

                    return View(model);
                }

                var dashboard = await response.Content
                    .ReadFromJsonAsync<OrganiserDashboardViewModel>();

                if (dashboard == null)
                {
                    ViewBag.Error =
                        "No dashboard information was received.";

                    return View(model);
                }

                return View(dashboard);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";

                return View(model);
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid dashboard information received.";

                return View(model);
            }
        }

        // ============================================
        // CAPTURE RACE RESULT - GET
        // ============================================

        [HttpGet]
        public async Task<IActionResult> CaptureResult(int eventId)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            var model = new ResultCaptureViewModel
            {
                EventId = eventId
            };

            try
            {
                using var eventRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}");

                using var eventResponse =
                    await _api.SendAsync(eventRequest);

                if (eventResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (eventResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (eventResponse.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Event not found.");

                if (!eventResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load event details.";

                    return View(model);
                }

                var evt = await eventResponse.Content
                    .ReadFromJsonAsync<EventViewModel>();

                model.EventName = evt?.EventName ?? "";

                using var enrolmentRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                using var enrolmentResponse =
                    await _api.SendAsync(enrolmentRequest);

                if (enrolmentResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (enrolmentResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!enrolmentResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load enrolments.";

                    return View(model);
                }

                model.Enrolments = await enrolmentResponse.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>()
                    ?? new List<EnrolmentViewModel>();

                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";

                return View(model);
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid information received.";

                return View(model);
            }
        }

        // ============================================
        // CAPTURE RACE RESULT - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(
            ResultCaptureViewModel model)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (model.EventId <= 0)
                return BadRequest("Invalid event ID.");

            if (!ModelState.IsValid)
            {
                await LoadCaptureDetails(model);

                return View(model);
            }

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Post,
                    $"api/events/{model.EventId}/results");

                request.Content = JsonContent.Create(new
                {
                    EnrolmentID = model.EnrolmentId,
                    FinishTime = model.FinishTime,
                    FinishPosition = model.FinishPosition,
                    ChipTime = model.ChipTime,
                    Status = model.Status
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "",
                        "A result already exists for this enrolment.");

                    await LoadCaptureDetails(model);

                    return View(model);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Race result captured successfully!";

                    return RedirectToAction(
                        "CaptureResult",
                        new { eventId = model.EventId });
                }

                ModelState.AddModelError(
                    "",
                    "Unable to capture result. Check your details.");

                await LoadCaptureDetails(model);

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Cannot connect to RaceDay API.");

                await LoadCaptureDetails(model);

                return View(model);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid response from RaceDay API.");

                await LoadCaptureDetails(model);

                return View(model);
            }
        }

        // ============================================
        // HELPER: LOAD CAPTURE DETAILS
        // ============================================

        private async Task LoadCaptureDetails(
            ResultCaptureViewModel model)
        {
            try
            {
                using var eventRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventId}");

                using var eventResponse =
                    await _api.SendAsync(eventRequest);

                if (eventResponse.IsSuccessStatusCode)
                {
                    var evt = await eventResponse.Content
                        .ReadFromJsonAsync<EventViewModel>();

                    model.EventName = evt?.EventName ?? "";
                }

                using var enrolmentRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventId}/enrolments");

                using var enrolmentResponse =
                    await _api.SendAsync(enrolmentRequest);

                if (enrolmentResponse.IsSuccessStatusCode)
                {
                    model.Enrolments = await enrolmentResponse.Content
                        .ReadFromJsonAsync<List<EnrolmentViewModel>>()
                        ?? new List<EnrolmentViewModel>();
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to reload event details.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid event information received.");
            }
        }

        // ============================================
        // MANAGE RACE RESULTS
        // ============================================

        [HttpGet]
        public async Task<IActionResult> ManageResults(int eventId)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            var model = new ResultManagementViewModel
            {
                EventId = eventId
            };

            try
            {
                using var eventRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}");

                using var eventResponse =
                    await _api.SendAsync(eventRequest);

                if (eventResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (eventResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (eventResponse.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Event not found.");

                if (!eventResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load event details.";

                    return View(model);
                }

                var evt = await eventResponse.Content
                    .ReadFromJsonAsync<EventViewModel>();

                model.EventName = evt?.EventName ?? "";

                // Verify organiser owns the event

                using var accessRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                using var accessResponse =
                    await _api.SendAsync(accessRequest);

                if (accessResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (accessResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!accessResponse.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502,
                        "Unable to verify access to this event.");
                }

                using var resultsRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/results");

                using var resultsResponse =
                    await _api.SendAsync(resultsRequest);

                if (resultsResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (resultsResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!resultsResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load race results.";

                    return View(model);
                }

                model.Results = await resultsResponse.Content
                    .ReadFromJsonAsync<List<RaceResultViewModel>>()
                    ?? new List<RaceResultViewModel>();

                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";

                return View(model);
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid result information received.";

                return View(model);
            }
        }

        // ============================================
        // EDIT RACE RESULT - GET
        // ============================================

        [HttpGet]
        public async Task<IActionResult> EditResult(
            int eventId,
            int resultId)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (eventId <= 0 || resultId <= 0)
                return BadRequest("Invalid event or result ID.");

            try
            {
                using var accessRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                using var accessResponse =
                    await _api.SendAsync(accessRequest);

                if (accessResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (accessResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!accessResponse.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502,
                        "Unable to verify event access.");
                }

                using var request = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/results");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502, "Unable to load results.");

                var results = await response.Content
                    .ReadFromJsonAsync<List<RaceResultViewModel>>();

                var result = results?
                    .FirstOrDefault(r => r.ResultID == resultId);

                if (result == null)
                    return NotFound("Result not found for this event.");

                var model = new ResultEditViewModel
                {
                    ResultID = result.ResultID,
                    EventId = eventId,
                    ParticipantName = result.ParticipantName,
                    FinishTime = result.FinishTime,
                    FinishPosition = result.FinishPosition,
                    ChipTime = result.ChipTime,
                    Status = result.Status
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    502,
                    "Cannot connect to RaceDay API.");
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    "Invalid result information received.");
            }
        }

        // ============================================
        // EDIT RACE RESULT - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(
            ResultEditViewModel model)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (model.EventId <= 0 || model.ResultID <= 0)
                return BadRequest("Invalid event or result ID.");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var accessRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventId}/enrolments");

                using var accessResponse =
                    await _api.SendAsync(accessRequest);

                if (accessResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (accessResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!accessResponse.IsSuccessStatusCode)
                {
                    return StatusCode(
                        502,
                        "Unable to verify event access.");
                }

                using var listRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventId}/results");

                using var listResponse =
                    await _api.SendAsync(listRequest);

                if (listResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (!listResponse.IsSuccessStatusCode)
                    return StatusCode(502, "Unable to verify result.");

                var results = await listResponse.Content
                    .ReadFromJsonAsync<List<RaceResultViewModel>>();

                var existing = results?
                    .FirstOrDefault(r => r.ResultID == model.ResultID);

                if (existing == null)
                    return NotFound("Result not found for this event.");

                using var request = CreateApiRequest(
                    HttpMethod.Put,
                    $"api/results/{model.ResultID}");

                request.Content = JsonContent.Create(new
                {
                    FinishTime = model.FinishTime,
                    FinishPosition = model.FinishPosition,
                    ChipTime = model.ChipTime,
                    Status = model.Status
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Race result updated successfully!";

                    return RedirectToAction(
                        "ManageResults",
                        new { eventId = model.EventId });
                }

                ModelState.AddModelError(
                    "",
                    "Unable to update result. Check your details.");

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "Cannot connect to RaceDay API.");

                return View(model);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid response from RaceDay API.");

                return View(model);
            }
        }

        // ============================================
        // STEP 31: UPLOAD EVENT BANNER - GET
        // ============================================

        [HttpGet]
        public IActionResult UploadEventBanner(int eventId)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            ViewBag.EventId = eventId;

            return View();
        }

        // ============================================
        // STEP 31: UPLOAD EVENT BANNER - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadEventBanner(
            int eventId,
            IFormFile? image)
        {
            var auth = CheckOrganiser();

            if (auth != null)
                return auth;

            if (eventId <= 0)
                return BadRequest("Invalid event ID.");

            ViewBag.EventId = eventId;

            if (image == null || image.Length == 0)
            {
                ViewBag.Error = "Please select an image.";

                return View();
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                ViewBag.Error = "Image size must not exceed 5 MB.";

                return View();
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                image.ContentType.ToLowerInvariant()))
            {
                ViewBag.Error =
                    "Only JPG, PNG and WebP images are allowed.";

                return View();
            }

            try
            {
                using var content = new MultipartFormDataContent();

                using var stream = image.OpenReadStream();

                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        image.ContentType);

                content.Add(
                    fileContent,
                    "image",
                    image.FileName);

                using var request = CreateApiRequest(
                    HttpMethod.Post,
                    $"api/events/{eventId}/banner");

                request.Content = content;

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Event banner uploaded successfully!";

                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    ViewBag.Error =
                        "Event not found or organiser profile missing.";
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ViewBag.Error =
                        "Invalid image. Please choose a JPG, PNG or WebP file.";
                }
                else
                {
                    ViewBag.Error =
                        "Unable to upload event banner. " +
                        "Check that the API and Azurite are running.";
                }

                return View();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Cannot connect to RaceDay API.";

                return View();
            }
        }
    }
}
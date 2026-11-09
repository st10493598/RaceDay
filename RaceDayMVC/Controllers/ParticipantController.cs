
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly HttpClient _api;

        public ParticipantController(
            IHttpClientFactory httpClientFactory)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        private IActionResult? CheckParticipant()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("Role") != "Participant")
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

        // GET: /Participant
        [HttpGet]
        public IActionResult Index()
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            return View();
        }

        // GET: /Participant/BrowseEvents
        [HttpGet]
        public async Task<IActionResult> BrowseEvents()
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, "api/events");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

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
                ViewBag.Error = "Invalid event information received.";
                return View(new List<EventViewModel>());
            }
        }

        // GET: /Participant/RegisterEvent/7
        [HttpGet]
        public async Task<IActionResult> RegisterEvent(int id)
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            if (id <= 0)
                return BadRequest("Invalid event ID.");

            try
            {
                using var eventRequest = CreateApiRequest(
                    HttpMethod.Get, $"api/events/{id}");

                using var eventResponse =
                    await _api.SendAsync(eventRequest);

                if (eventResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (eventResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (eventResponse.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Event not found.");

                if (!eventResponse.IsSuccessStatusCode)
                    return StatusCode(502, "Unable to load event.");

                var evt = await eventResponse.Content
                    .ReadFromJsonAsync<EventViewModel>();

                if (evt == null)
                    return NotFound("Event not found.");

                using var categoryRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{id}/categories");

                using var categoryResponse =
                    await _api.SendAsync(categoryRequest);

                if (categoryResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (categoryResponse.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!categoryResponse.IsSuccessStatusCode)
                    return StatusCode(502, "Unable to load categories.");

                var categories = await categoryResponse.Content
                    .ReadFromJsonAsync<List<ParticipantCategoryViewModel>>();

                var model = new ParticipantRegistrationViewModel
                {
                    EventId = id,
                    EventName = evt.EventName,
                    Categories = categories ??
                        new List<ParticipantCategoryViewModel>()
                };

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
                    "Invalid event or category information.");
            }
        }

        // POST: /Participant/RegisterEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEvent(
            ParticipantRegistrationViewModel model)
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            if (model.EventId <= 0)
                return BadRequest("Invalid event ID.");

            if (!ModelState.IsValid)
            {
                await LoadRegistrationDetails(model);
                return View(model);
            }

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Post,
                    $"api/events/{model.EventId}/enrolments");

                request.Content = JsonContent.Create(new
                {
                    CategoryID = model.CategoryId
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
                        "You are already enrolled in this event.");

                    await LoadRegistrationDetails(model);
                    return View(model);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "You have successfully registered for the event!";

                    return RedirectToAction("BrowseEvents");
                }

                ModelState.AddModelError(
                    "",
                    "Registration failed. Please check your selection.");

                await LoadRegistrationDetails(model);
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API.");

                await LoadRegistrationDetails(model);
                return View(model);
            }
        }

        private async Task LoadRegistrationDetails(
            ParticipantRegistrationViewModel model)
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

                using var categoryRequest = CreateApiRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventId}/categories");

                using var categoryResponse =
                    await _api.SendAsync(categoryRequest);

                if (categoryResponse.IsSuccessStatusCode)
                {
                    model.Categories =
                        await categoryResponse.Content.ReadFromJsonAsync<
                            List<ParticipantCategoryViewModel>>()
                        ?? new List<ParticipantCategoryViewModel>();
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to reload registration details.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    "", "Invalid registration details received.");
            }
        }

        // GET: /Participant/MyEnrolments
        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, "api/enrolments");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load your enrolments.";
                    return View(new List<EnrolmentViewModel>());
                }

                var enrolments = await response.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>();

                return View(
                    enrolments ?? new List<EnrolmentViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Cannot connect to RaceDay API.";
                return View(new List<EnrolmentViewModel>());
            }
            catch (JsonException)
            {
                ViewBag.Error = "Invalid enrolment information received.";
                return View(new List<EnrolmentViewModel>());
            }
        }

        // GET: /Participant/MyResults
        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            var results = new List<ParticipantResultViewModel>();

            try
            {
                using var enrolmentRequest = CreateApiRequest(
                    HttpMethod.Get, "api/enrolments");

                using var enrolmentResponse =
                    await _api.SendAsync(enrolmentRequest);

                if (enrolmentResponse.StatusCode ==
                    HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (enrolmentResponse.StatusCode ==
                    HttpStatusCode.Forbidden)
                    return Forbid();

                if (!enrolmentResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Unable to load your registered events.";

                    return View(results);
                }

                var enrolments = await enrolmentResponse.Content
                    .ReadFromJsonAsync<List<EnrolmentViewModel>>()
                    ?? new List<EnrolmentViewModel>();

                foreach (var enrolment in enrolments)
                {
                    using var resultRequest = CreateApiRequest(
                        HttpMethod.Get,
                        $"api/enrolments/{enrolment.EnrolmentId}/result");

                    using var resultResponse =
                        await _api.SendAsync(resultRequest);

                    if (resultResponse.StatusCode ==
                        HttpStatusCode.Unauthorized)
                        return SessionExpired();

                    if (resultResponse.StatusCode ==
                        HttpStatusCode.Forbidden)
                        return Forbid();

                    if (resultResponse.StatusCode ==
                        HttpStatusCode.NotFound)
                        continue;

                    if (!resultResponse.IsSuccessStatusCode)
                    {
                        ViewBag.Error =
                            "Some results could not be loaded.";
                        continue;
                    }

                    var result = await resultResponse.Content
                        .ReadFromJsonAsync<ParticipantResultViewModel>();

                    if (result == null)
                        continue;

                    result.EventName = enrolment.EventName;
                    result.EventDate = enrolment.EventDate;
                    result.CategoryName = enrolment.CategoryName;

                    results.Add(result);
                }

                return View(results);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Cannot connect to RaceDay API. Please try again.";

                return View(results);
            }
            catch (JsonException)
            {
                ViewBag.Error =
                    "Invalid result information received from the API.";

                return View(results);
            }
        }

        // GET: /Participant/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Get, "api/users/profile");

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Unable to load your profile.";
                    return RedirectToAction("Index");
                }

                var profile = await response.Content
                    .ReadFromJsonAsync<ParticipantProfileViewModel>();

                if (profile == null)
                {
                    TempData["Error"] =
                        "Profile information was not found.";

                    return RedirectToAction("Index");
                }

                return View(profile);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Cannot connect to RaceDay API.";
                return RedirectToAction("Index");
            }
            catch (JsonException)
            {
                TempData["Error"] =
                    "Invalid profile information received.";

                return RedirectToAction("Index");
            }
        }

        // POST: /Participant/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            ParticipantProfileViewModel model)
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = CreateApiRequest(
                    HttpMethod.Put, "api/users/profile");

                request.Content = JsonContent.Create(new
                {
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    dateOfBirth = model.DateOfBirth,
                    gender = model.Gender,
                    contactNumber = model.ContactNumber
                });

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Profile updated successfully!";

                    return RedirectToAction("Profile");
                }

                ModelState.AddModelError(
                    "", "Unable to update your profile.");

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API.");

                return View(model);
            }
        }

        // POST: /Participant/UploadProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile? image)
        {
            var auth = CheckParticipant();
            if (auth != null) return auth;

            if (image == null || image.Length == 0)
            {
                TempData["Error"] =
                    "Please select a profile picture.";

                return RedirectToAction("Profile");
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (image.Length > maxFileSize)
            {
                TempData["Error"] =
                    "Image must not exceed 5 MB.";

                return RedirectToAction("Profile");
            }

            if (image.ContentType != "image/jpeg" &&
                image.ContentType != "image/png" &&
                image.ContentType != "image/webp")
            {
                TempData["Error"] =
                    "Only JPG, PNG and WebP images are allowed.";

                return RedirectToAction("Profile");
            }

            try
            {
                using var formData = new MultipartFormDataContent();

                await using var imageStream = image.OpenReadStream();

                using var imageContent = new StreamContent(imageStream);

                imageContent.Headers.ContentType =
                    new MediaTypeHeaderValue(image.ContentType);

                formData.Add(
                    imageContent,
                    "image",
                    image.FileName);

                using var request = CreateApiRequest(
                    HttpMethod.Post, "api/users/profile/picture");

                request.Content = formData;

                using var response = await _api.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                    return Forbid();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Profile picture uploaded successfully!";
                }
                else
                {
                    // Show the actual API error during local testing
                    var errorDetails =
                        await response.Content.ReadAsStringAsync();

                    TempData["Error"] =
                        $"Upload failed (HTTP {(int)response.StatusCode}): " +
                        errorDetails;
                }

                return RedirectToAction("Profile");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Cannot connect to RaceDay API.";

                return RedirectToAction("Profile");
            }
        }
    }
}

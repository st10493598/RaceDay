
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _api;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        // Display login page
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // Process login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await _api.PostAsJsonAsync(
                    "api/Auth/login", model);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "", "Invalid email or password.");
                    return View(model);
                }

                using var json = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync());

                var user = json.RootElement;

                int userId = user.GetProperty("userId").GetInt32();
                string role = user.GetProperty("role").GetString() ?? "";
                string email = user.GetProperty("email").GetString() ?? "";
                string firstName = user.GetProperty("firstName").GetString() ?? "";

                if (role != "Organiser" && role != "Participant")
                {
                    ModelState.AddModelError("", "Invalid user role.");
                    return View(model);
                }

                // Retrieve the API session cookie from the login response.
                string? apiSessionCookie = null;

                if (response.Headers.TryGetValues(
                    "Set-Cookie", out var cookieHeaders))
                {
                    foreach (string cookieHeader in cookieHeaders)
                    {
                        if (cookieHeader.StartsWith(
                            ".AspNetCore.Session=",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            apiSessionCookie = cookieHeader.Split(';')[0];
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(apiSessionCookie))
                {
                    ModelState.AddModelError(
                        "", "API session could not be established.");
                    return View(model);
                }

                // Clear any previous MVC login session.
                HttpContext.Session.Clear();

                // Save the API cookie in the server-side MVC session.
                HttpContext.Session.SetString(
                    "ApiSessionCookie", apiSessionCookie);

                // Save user details for role-based navigation.
                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("FirstName", firstName);

                if (role == "Organiser")
                    return RedirectToAction("Index", "Organiser");

                return RedirectToAction("Index", "Participant");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API. Please try again.");
                return View(model);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    "", "The API returned an invalid login response.");
                return View(model);
            }
            catch (KeyNotFoundException)
            {
                ModelState.AddModelError(
                    "", "The API login response is missing user information.");
                return View(model);
            }
        }

        // Display registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // Process registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role != "Organiser" &&
                model.Role != "Participant")
            {
                ModelState.AddModelError(
                    "Role", "Select a valid role.");
            }

            if (model.Role == "Organiser" &&
                string.IsNullOrWhiteSpace(model.OrganisationName))
            {
                ModelState.AddModelError(
                    "OrganisationName",
                    "Organisation name is required.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await _api.PostAsJsonAsync(
                    "api/Auth/register", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Registration successful! Please log in.";

                    return RedirectToAction("Login");
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "Email", "This email is already registered.");
                }
                else
                {
                    ModelState.AddModelError(
                        "", "Registration failed. Please check your details.");
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Cannot connect to RaceDay API. Please try again.");
            }

            return View(model);
        }

        // Log out
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? apiSessionCookie =
                HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                if (!string.IsNullOrWhiteSpace(apiSessionCookie))
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post, "api/Auth/logout");

                    request.Headers.TryAddWithoutValidation(
                        "Cookie", apiSessionCookie);

                    using var response = await _api.SendAsync(request);
                }
            }
            catch (HttpRequestException)
            {
                // Clear the MVC session even if the API is unavailable.
            }
            finally
            {
                HttpContext.Session.Clear();
            }

            return RedirectToAction("Login");
        }
    }
}

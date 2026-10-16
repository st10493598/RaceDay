using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.API.Tests
{
    public class EventsControllerTests
    {
        // Helper: Creates a fresh in-memory database for each test
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Helper: Attaches a session with a specific UserID and Role
        // (FIXED: Uses TestSession which stores values as real bytes)
        private void AttachSession(EventsController controller, int userId, string role)
        {
            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.SetInt32("UserID", userId);
            session.SetString("Role", role);
            httpContext.Session = session;
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        // Helper: Seeds a user + organiser into the database
        private async Task<(User user, Organiser organiser)> SeedOrganiser(ApplicationDbContext context)
        {
            var user = new User
            {
                Email = "org@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Organiser",
                Role = "Organiser"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var organiser = new Organiser
            {
                UserID = user.UserID,
                OrganisationName = "Test Events"
            };

            context.Organisers.Add(organiser);
            await context.SaveChangesAsync();

            return (user, organiser);
        }

        // ============ CREATE EVENT TESTS ============

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201Created()
        {
            var context = GetInMemoryDbContext();
            var (user, _) = await SeedOrganiser(context);

            var controller = new EventsController(context);
            AttachSession(controller, user.UserID, "Organiser");

            var dto = new EventCreateDto
            {
                EventName = "Test Marathon",
                Description = "A test event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Cape Town",
                Distance = 42.20m,
                EventType = "Running"
            };

            var result = await controller.CreateEvent(dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, statusResult.StatusCode);
            Assert.Single(context.Events);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_Returns403Forbidden()
        {
            var context = GetInMemoryDbContext();

            var user = new User
            {
                Email = "part@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Participant",
                Role = "Participant"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.Participants.Add(new Participant
            {
                UserID = user.UserID
            });

            await context.SaveChangesAsync();

            var controller = new EventsController(context);
            AttachSession(controller, user.UserID, "Participant");

            var dto = new EventCreateDto
            {
                EventName = "Hacked Event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Nowhere",
                Distance = 10m,
                EventType = "Running"
            };

            var result = await controller.CreateEvent(dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
            Assert.Empty(context.Events);
        }

        [Fact]
        public async Task CreateEvent_WithoutLogin_Returns401Unauthorized()
        {
            var context = GetInMemoryDbContext();
            var controller = new EventsController(context);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var dto = new EventCreateDto
            {
                EventName = "Test",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Test",
                Distance = 10m,
                EventType = "Running"
            };

            var result = await controller.CreateEvent(dto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        // ============ VIEW EVENT TESTS ============

        [Fact]
        public async Task GetAllEvents_WithoutLogin_Returns401Unauthorized()
        {
            var context = GetInMemoryDbContext();
            var controller = new EventsController(context);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await controller.GetAllEvents();

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task GetAllEvents_AsLoggedInUser_Returns200Ok()
        {
            var context = GetInMemoryDbContext();
            var (user, organiser) = await SeedOrganiser(context);

            context.Events.Add(new Event
            {
                OrganiserID = organiser.OrganiserID,
                EventName = "Test Event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Test Location",
                Distance = 10m,
                EventType = "Running"
            });

            await context.SaveChangesAsync();

            var controller = new EventsController(context);
            AttachSession(controller, user.UserID, "Organiser");

            var result = await controller.GetAllEvents();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        // ============ DELETE EVENT TESTS ============

        [Fact]
        public async Task DeleteEvent_ByWrongOrganiser_Returns403Forbidden()
        {
            var context = GetInMemoryDbContext();

            var (firstUser, firstOrganiser) = await SeedOrganiser(context);

            var evt = new Event
            {
                OrganiserID = firstOrganiser.OrganiserID,
                EventName = "First Org's Event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Location",
                Distance = 10m,
                EventType = "Running"
            };

            context.Events.Add(evt);
            await context.SaveChangesAsync();

            var secondUser = new User
            {
                Email = "org2@test.com",
                PasswordHash = "hash",
                FirstName = "Other",
                LastName = "Organiser",
                Role = "Organiser"
            };

            context.Users.Add(secondUser);
            await context.SaveChangesAsync();

            context.Organisers.Add(new Organiser
            {
                UserID = secondUser.UserID,
                OrganisationName = "Other Events"
            });

            await context.SaveChangesAsync();

            var controller = new EventsController(context);
            AttachSession(controller, secondUser.UserID, "Organiser");

            var result = await controller.DeleteEvent(evt.EventID);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
            Assert.Single(context.Events);
        }
    }
}
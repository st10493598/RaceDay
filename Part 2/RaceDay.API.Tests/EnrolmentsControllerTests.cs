
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
    public class EnrolmentsControllerTests
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
        private void AttachSession(EnrolmentsController controller, int userId, string role)
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

        // Helper: Seeds an Event + Category + Organiser
        private async Task<(User user, Organiser organiser, Event evt, Category category)>
            SeedEventWithCategory(ApplicationDbContext context)
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

            var evt = new Event
            {
                OrganiserID = organiser.OrganiserID,
                EventName = "Test Marathon",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Cape Town",
                Distance = 42.20m,
                EventType = "Running"
            };

            context.Events.Add(evt);
            await context.SaveChangesAsync();

            var category = new Category
            {
                EventID = evt.EventID,
                CategoryName = "Men 18-39",
                Description = "Male participants aged 18 to 39."
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            return (user, organiser, evt, category);
        }

        // Helper: Seeds a Participant
        private async Task<(User user, Participant participant)>
            SeedParticipant(ApplicationDbContext context)
        {
            var user = new User
            {
                Email = "part@test.com",
                PasswordHash = "hash",
                FirstName = "Alice",
                LastName = "Johnson",
                Role = "Participant"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var participant = new Participant
            {
                UserID = user.UserID,
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Female",
                ContactNumber = "0821234567"
            };

            context.Participants.Add(participant);
            await context.SaveChangesAsync();

            return (user, participant);
        }

        // ============ CREATE ENROLMENT TESTS ============

        [Fact]
        public async Task Enrol_AsParticipant_Returns201Created()
        {
            var context = GetInMemoryDbContext();

            var (_, _, evt, category) =
                await SeedEventWithCategory(context);

            var (participantUser, _) =
                await SeedParticipant(context);

            var controller = new EnrolmentsController(context);
            AttachSession(controller, participantUser.UserID, "Participant");

            var dto = new EnrolmentCreateDto
            {
                CategoryID = category.CategoryID
            };

            var result = await controller.CreateEnrolment(evt.EventID, dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, statusResult.StatusCode);
            Assert.Single(context.Enrolments);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_Returns403Forbidden()
        {
            var context = GetInMemoryDbContext();

            var (organiserUser, _, evt, category) =
                await SeedEventWithCategory(context);

            var controller = new EnrolmentsController(context);
            AttachSession(controller, organiserUser.UserID, "Organiser");

            var dto = new EnrolmentCreateDto
            {
                CategoryID = category.CategoryID
            };

            var result = await controller.CreateEnrolment(evt.EventID, dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
            Assert.Empty(context.Enrolments);
        }

        [Fact]
        public async Task Enrol_WithoutLogin_Returns401Unauthorized()
        {
            var context = GetInMemoryDbContext();

            var (_, _, evt, category) =
                await SeedEventWithCategory(context);

            var controller = new EnrolmentsController(context);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var dto = new EnrolmentCreateDto
            {
                CategoryID = category.CategoryID
            };

            var result = await controller.CreateEnrolment(evt.EventID, dto);

            var unauthorizedResult =
                Assert.IsType<UnauthorizedObjectResult>(result);

            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Enrol_InNonexistentEvent_Returns404NotFound()
        {
            var context = GetInMemoryDbContext();

            var (_, _, _, category) =
                await SeedEventWithCategory(context);

            var (participantUser, _) =
                await SeedParticipant(context);

            var controller = new EnrolmentsController(context);
            AttachSession(controller, participantUser.UserID, "Participant");

            var dto = new EnrolmentCreateDto
            {
                CategoryID = category.CategoryID
            };

            // Event ID 999 does not exist
            var result = await controller.CreateEnrolment(999, dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Empty(context.Enrolments);
        }

        // ============ VIEW MY ENROLMENTS TESTS ============

        [Fact]
        public async Task GetMyEnrolments_AsParticipant_Returns200Ok()
        {
            var context = GetInMemoryDbContext();

            var (_, _, evt, category) =
                await SeedEventWithCategory(context);

            var (participantUser, participant) =
                await SeedParticipant(context);

            // Add an enrolment directly
            context.Enrolments.Add(new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                EventID = evt.EventID,
                CategoryID = category.CategoryID,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            });

            await context.SaveChangesAsync();

            var controller = new EnrolmentsController(context);
            AttachSession(controller, participantUser.UserID, "Participant");

            var result = await controller.GetMyEnrolments();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetMyEnrolments_AsOrganiser_Returns403Forbidden()
        {
            var context = GetInMemoryDbContext();

            var (organiserUser, _, _, _) =
                await SeedEventWithCategory(context);

            var controller = new EnrolmentsController(context);
            AttachSession(controller, organiserUser.UserID, "Organiser");

            var result = await controller.GetMyEnrolments();

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }
    }
}


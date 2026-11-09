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
    public class AuthControllerTests
    {
        // Helper: Creates a fresh in-memory database for each test
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // Helper: Attaches an empty session to the controller
        private void AttachSession(AuthController controller)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        // ============ REGISTER TESTS ============

        [Fact]
        public async Task Register_WithValidOrganiser_Returns201Created()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            var dto = new RegisterDto
            {
                Email = "test.organiser@email.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "Organiser",
                Role = "Organiser",
                OrganisationName = "Test Events"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, statusResult.StatusCode);
            Assert.Single(context.Users);
            Assert.Single(context.Organisers);
            Assert.Equal("test.organiser@email.com", context.Users.First().Email);
        }

        [Fact]
        public async Task Register_WithValidParticipant_Returns201Created()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            var dto = new RegisterDto
            {
                Email = "test.participant@email.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "Participant",
                Role = "Participant"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, statusResult.StatusCode);
            Assert.Single(context.Users);
            Assert.Single(context.Participants);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409Conflict()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            // First user
            context.Users.Add(new User
            {
                Email = "duplicate@email.com",
                PasswordHash = "hash",
                FirstName = "First",
                LastName = "User",
                Role = "Participant"
            });
            await context.SaveChangesAsync();

            // Try to register with the same email
            var dto = new RegisterDto
            {
                Email = "duplicate@email.com",
                Password = "Password123!",
                FirstName = "Second",
                LastName = "User",
                Role = "Participant"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);
        }

        // ============ LOGIN TESTS ============

        [Fact]
        public async Task Login_WithCorrectCredentials_Returns200Ok()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            // Create a user with hashed password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
            context.Users.Add(new User
            {
                Email = "login@test.com",
                PasswordHash = passwordHash,
                FirstName = "Login",
                LastName = "Tester",
                Role = "Participant"
            });
            await context.SaveChangesAsync();

            var dto = new LoginDto
            {
                Email = "login@test.com",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401Unauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
            context.Users.Add(new User
            {
                Email = "wrongpass@test.com",
                PasswordHash = passwordHash,
                FirstName = "Wrong",
                LastName = "Password",
                Role = "Participant"
            });
            await context.SaveChangesAsync();

            var dto = new LoginDto
            {
                Email = "wrongpass@test.com",
                Password = "WrongPassword!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AuthController(context);
            AttachSession(controller);

            var dto = new LoginDto
            {
                Email = "doesnotexist@test.com",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }
    }
}

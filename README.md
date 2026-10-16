# RaceDay Event Management System

## Project Description

RaceDay is a web-based event management system designed for running, walking, and cycling events in South Africa. The system allows event organisers to manage events and participants to enrol in events and access race-related information.

The project is developed using ASP.NET Core, Entity Framework Core, and SQL Server.

## User Roles

### Organiser
- Create and manage events.
- Manage event categories.
- Access event-related information.

### Participant
- Manage participant profile information.
- Enrol in available events.
- Access race-related information and results.

---

## Part 1: Planning and Database Design

Part 1 focused on planning the RaceDay system, designing the database, documenting the API endpoints, and setting up continuous integration.

### Part 1 Deliverables

- **Entity Relationship Diagram (ERD):** `docs/RaceDay_ERD.pdf`
- **API Endpoint Plan:** `docs/RaceDay_API_Endpoint_Plan.pdf`
- **SQL Database Script:** `docs/RaceDay_Database.sql`
- **CI/CD Workflow:** `.github/workflows/part1-ci.yml`
- **Successful CI Build Screenshot:** `images/ci-build-success.png`

### Part 1 YouTube Video

[**YOUR_UNLISTED_VIDEO_LINK**](https://www.youtube.com/your-unlisted-link)

---

## Part 2: RESTful API Development

Part 2 focused on developing the RaceDay REST API using ASP.NET Core Web API, Entity Framework Core, and SQL Server.

### Technologies Used

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- BCrypt password hashing
- Swagger / OpenAPI
- Unit testing

### Main Features

- User authentication and account management.
- Organiser and Participant roles.
- Event creation and management.
- Event category management.
- Participant enrolments.
- Race result management.
- Database integration using Entity Framework Core.
- API documentation and testing using Swagger.
- Unit tests for important application functionality.

### Controllers

The API includes the following controllers:

- **AuthController:** Handles authentication.
- **UsersController:** Handles user-related operations.
- **EventsController:** Manages events.
- **CategoriesController:** Manages event categories.
- **EnrolmentsController:** Manages event enrolments.
- **ResultsController:** Handles race results.

### Data Models

The main models include:

- `User`
- `Organiser`
- `Participant`
- `Event`
- `Category`

These models represent the main entities in the RaceDay system. They include data annotations for validation and properties that represent relationships between related entities.

### Database Integration

Entity Framework Core is used to communicate with SQL Server through `ApplicationDbContext`.

The database stores the information required for users, events, categories, enrolments, and race results.

### Security

The project uses BCrypt password hashing to avoid storing passwords in plain text. The application also includes authentication and role-related handling for Organisers and Participants.

### Running the API

1. Open the solution file `Part 2/RaceDay.API.slnx` in Visual Studio.
2. Ensure SQL Server is running and the `RaceDayDB` database is configured.
3. Check that the database connection string is correct.
4. Set `RaceDay.API` as the startup project.
5. Run the application.
6. Open Swagger at `https://localhost:7019/swagger` if this is the address configured for your application.

Swagger provides an interactive interface for viewing endpoints, sending requests, and inspecting API responses.

### API Testing

The API can be tested through Swagger by selecting an endpoint, providing any required input, executing the request, and reviewing the response and HTTP status code.

### Unit Testing

The project includes unit tests covering important functionality, including authentication, events, and enrolments.

Tests can be run in Visual Studio using **Test Explorer** by selecting **Run All**. The test results indicate which tests pass or fail.

### Part 2 CI/CD

The Part 2 continuous integration workflow is located at:

`.github/workflows/part2-ci.yml`

The workflow supports automated build and testing checks.

The successful CI build screenshot is located at:

`images/part2-ci-success.png`

### Part 2 YouTube Video Presentation

Watch my RaceDay Part 2 REST API demonstration:

[**Watch the Part 2 Presentation on YouTube**](https://youtu.be/wxo49rCVK84)

This video demonstrates the RaceDay project structure, data models, controllers, database integration, Swagger endpoints, and unit testing.

Replace the placeholder above with your actual unlisted YouTube video URL.

---

## Repository Structure

```text
RaceDay-main/
├── .github/
│   └── workflows/
│       ├── part1-ci.yml
│       └── part2-ci.yml
├── docs/
│   ├── RaceDay_ERD.pdf
│   ├── RaceDay_API_Endpoint_Plan.pdf
│   └── RaceDay_Database.sql
├── images/
│   ├── ci-build-success.png
│   └── part2-ci-success.png
├── Part 2/
│   ├── RaceDay.API.slnx
│   └── RaceDay.API/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Models/
│       └── Program.cs
├── README.md
└── .gitignore
```

## Version Control

Git and GitHub are used to manage the source code and track changes. The repository includes CI/CD workflows to support automated build and testing checks.

## Conclusion

The RaceDay project demonstrates system planning, database design, and REST API development for an event management system. Part 1 focused on the system design and database documentation, while Part 2 focused on implementing the REST API, integrating the database, documenting endpoints, and testing the application.

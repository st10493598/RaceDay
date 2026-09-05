# RaceDay Event Management System


## System Description

RaceDay is a web-based event management system designed to support the planning and management of running, walking and cycling events. The system provides separate functionality for Organisers and Participants.

Organisers can create and manage events, define event categories, view participant enrolments and record race results. Participants can browse available events, view event details, enrol in events, manage their enrolments and view their results.

The system is planned around a relational database and a RESTful API. The database stores users, organisers, participants, events, categories, enrolments and results while maintaining relationships between these entities through primary and foreign keys. The API endpoint plan defines how users and the system will interact with these resources securely according to their assigned roles.


## User Roles

### Organiser

Organisers are the creators and administrators of events. They have the following key responsibilities:

* Create, edit, and delete their own events
* Create and manage categories (age groups, gender groups) for their events
* View all participants enrolled in their events
* Capture and update participant finish times and positions for their events

### Participant

Participants are the athletes who sign up for events. They can perform the following actions:

* Create a profile and log in to the system
* Browse and view details of all available events
* Enrol for events and select a specific category to compete in
* View their own enrolment history
* View their own race results and performance history

## Part 1 Deliverables

This repository contains the foundational planning and design work for the RaceDay system.

### Entity Relationship Diagram (ERD)

The ERD, located at `docs/RaceDay_ERD.pdf`, defines the core data structure for RaceDay. It identifies the necessary entities (User, Organiser, Participant, Event, Category, Enrolment, Result), their attributes, primary and foreign keys, and the relationships, including cardinality, between them. This document serves as the blueprint for the database design.

### API Endpoint Plan

The API Endpoint Plan, located at `docs/RaceDay_API_Endpoint_Plan.pdf`, outlines the future RESTful API. It details each endpoint's HTTP method, route, description, required role, request body, and expected responses. This plan ensures that the API will support all required client-side functionality.

### SQL Database Script

The SQL script, located at `docs/RaceDay_Database.sql`, is a working database implementation based on the ERD. It includes:

* CREATE TABLE statements for all 7 entities
* Primary key, foreign key, and other constraints
* NOT NULL, UNIQUE, CHECK, and DEFAULT constraints
* Realistic seed data, including 2 Organisers, 2 Participants, 3 Events, and sample Enrolments and Results

## Repository Structure

```text
RaceDay/
├── .github/
│   └── workflows/
│       └── part1-ci.yml
├── docs/
│   ├── RaceDay_ERD.pdf
│   ├── RaceDay_API_Endpoint_Plan.pdf
│   └── RaceDay_Database.sql
├── images/
│   └── ci-build-success.png
├── README.md
└── .gitignore
```

## Database Setup

To set up the RaceDay database on your local SQL Server instance:

1. Ensure you have SQL Server and SQL Server Management Studio (SSMS) installed.
2. Open SSMS and connect to your local SQL Server instance.
3. Open the `docs/RaceDay_Database.sql` script.
4. Execute the entire script by pressing F5.
5. This will create the `RaceDayDB` database, all tables, and populate them with sample data.
6. Verify the creation by running queries such as:

```sql
SELECT * FROM [User];
SELECT * FROM Organiser;
SELECT * FROM Participant;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;
```

## CI/CD

This repository uses GitHub Actions for Continuous Integration (CI) to validate the Part 1 submission structure.

### Workflow

`.github/workflows/part1-ci.yml`

### What it checks

The workflow ensures that:

* The `docs` folder exists
* `RaceDay_ERD.pdf` exists
* `RaceDay_API_Endpoint_Plan.pdf` exists
* `RaceDay_Database.sql` exists
* `README.md` exists

### Successful CI Build

The CI workflow will display a successful green check once all required files are present.
### Successful CI Build

![Part 1 CI/CD Build Success](images/ci-build-success.png)


## Video Demonstration

An unlisted YouTube video explaining the planning decisions and demonstrating the SQL script running live will be added here:

**Video link:** To be added

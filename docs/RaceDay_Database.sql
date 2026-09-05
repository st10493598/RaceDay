-- ============================================
-- RaceDay Event Management System
-- Part 1 - Database Creation Script
-- ============================================

-- ============================================
-- RESET DATABASE IF IT ALREADY EXISTS
-- ============================================

USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB
    SET SINGLE_USER
    WITH ROLLBACK IMMEDIATE;

    DROP DATABASE RaceDayDB;
END;
GO

-- ============================================
-- CREATE THE RACE DAY DATABASE
-- ============================================

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ============================================
-- 1. USER TABLE
-- ============================================

CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL
        CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================
-- 2. ORGANISER TABLE
-- ============================================

CREATE TABLE Organiser (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    OrganisationName NVARCHAR(255) NULL,

    CONSTRAINT FK_Organiser_User
        FOREIGN KEY (UserID)
        REFERENCES [User](UserID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- 3. PARTICIPANT TABLE
-- ============================================

CREATE TABLE Participant (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT UNIQUE NOT NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(20) NULL,
    ContactNumber NVARCHAR(20) NULL,

    CONSTRAINT FK_Participant_User
        FOREIGN KEY (UserID)
        REFERENCES [User](UserID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- 4. EVENT TABLE
-- ============================================

CREATE TABLE Event (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(255) NOT NULL,
    Distance DECIMAL(10,2) NOT NULL,
    EventType NVARCHAR(50) NOT NULL
        CHECK (EventType IN ('Running', 'Walking', 'Cycling')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Event_Organiser
        FOREIGN KEY (OrganiserID)
        REFERENCES Organiser(OrganiserID)
        ON DELETE CASCADE,

    CONSTRAINT CK_Event_Distance
        CHECK (Distance > 0)
);
GO

-- ============================================
-- 5. CATEGORY TABLE
-- ============================================

CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,

    CONSTRAINT FK_Category_Event
        FOREIGN KEY (EventID)
        REFERENCES Event(EventID)
        ON DELETE CASCADE,

    CONSTRAINT UQ_Category_Event_CategoryName
        UNIQUE (EventID, CategoryName)
);
GO

-- ============================================
-- 6. ENROLMENT TABLE
-- ============================================

CREATE TABLE Enrolment (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),

    CONSTRAINT FK_Enrolment_Participant
        FOREIGN KEY (ParticipantID)
        REFERENCES Participant(ParticipantID)
        ON DELETE CASCADE,

    CONSTRAINT FK_Enrolment_Event
        FOREIGN KEY (EventID)
        REFERENCES Event(EventID),

    CONSTRAINT FK_Enrolment_Category
        FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),

    CONSTRAINT UQ_Enrolment_Participant_Event
        UNIQUE (ParticipantID, EventID)
);
GO

-- ============================================
-- 7. RESULT TABLE
-- ============================================

CREATE TABLE Result (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT UNIQUE NOT NULL,
    FinishTime TIME NULL,
    FinishPosition INT NULL,
    ChipTime TIME NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'DNS'
        CHECK (Status IN ('DNS', 'DNF', 'Finished')),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Result_Enrolment
        FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID)
        ON DELETE CASCADE,

    CONSTRAINT CK_Result_FinishPosition
        CHECK (FinishPosition IS NULL OR FinishPosition > 0)
);
GO

-- ============================================
-- SEED DATA
-- ============================================

-- USERS
INSERT INTO [User]
    (Email, PasswordHash, FirstName, LastName, Role)
VALUES
    ('john.doe@organiser.com', 'hashed_password_1', 'John', 'Doe', 'Organiser'),
    ('jane.smith@organiser.com', 'hashed_password_2', 'Jane', 'Smith', 'Organiser'),
    ('alice.participant@email.com', 'hashed_password_3', 'Alice', 'Johnson', 'Participant'),
    ('bob.participant@email.com', 'hashed_password_4', 'Bob', 'Williams', 'Participant');
GO

-- ORGANISERS
INSERT INTO Organiser
    (UserID, OrganisationName)
VALUES
    (1, 'Cape Town Marathon Events'),
    (2, 'Durban Cycling Club');
GO

-- PARTICIPANTS
INSERT INTO Participant
    (UserID, DateOfBirth, Gender, ContactNumber)
VALUES
    (3, '1985-04-12', 'Female', '0821234567'),
    (4, '1990-11-25', 'Male', '0839876543');
GO

-- EVENTS
INSERT INTO Event
    (OrganiserID, EventName, Description, EventDate, Location, Distance, EventType)
VALUES
    (1, 'Cape Town Marathon 2026',
     'The premier running event in the Western Cape.',
     '2026-04-15 06:00:00',
     'Cape Town, South Africa',
     42.20,
     'Running'),

    (1, 'Two Oceans Ultra Marathon',
     '56km ultra marathon with stunning coastal views.',
     '2026-04-03 05:30:00',
     'Cape Town, South Africa',
     56.00,
     'Running'),

    (2, 'Durban World Cycle Tour',
     'A scenic cycling event along the Durban beachfront.',
     '2026-05-20 07:00:00',
     'Durban, South Africa',
     100.00,
     'Cycling');
GO

-- CATEGORIES
INSERT INTO Category
    (EventID, CategoryName, Description)
VALUES
    (1, 'Men 18-39',
     'Male participants aged 18 to 39.'),

    (1, 'Men 40-49',
     'Male participants aged 40 to 49.'),

    (1, 'Women 18-39',
     'Female participants aged 18 to 39.'),

    (1, 'Women 40-49',
     'Female participants aged 40 to 49.'),

    (2, 'Men Open',
     'All male participants.'),

    (2, 'Women Open',
     'All female participants.'),

    (3, 'Men 18-39',
     'Male cyclists aged 18 to 39.'),

    (3, 'Women 18-39',
     'Female cyclists aged 18 to 39.');
GO

-- ENROLMENTS
INSERT INTO Enrolment
    (ParticipantID, EventID, CategoryID, Status)
VALUES
    (1, 1, 4, 'Confirmed'),
    (1, 2, 6, 'Pending'),
    (2, 1, 1, 'Confirmed'),
    (2, 3, 7, 'Pending');
GO

-- RESULTS
INSERT INTO Result
    (EnrolmentID, FinishTime, FinishPosition, ChipTime, Status)
VALUES
    (1, '03:45:12', 150, '03:42:55', 'Finished'),
    (3, '04:00:01', 220, '03:58:20', 'Finished');
GO

-- ============================================
-- VERIFY THE DATABASE
-- ============================================

SELECT * FROM [User];
SELECT * FROM Organiser;
SELECT * FROM Participant;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;
GO

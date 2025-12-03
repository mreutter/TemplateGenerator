-- Deletes old Database and creates new one
DROP DATABASE IF EXISTS schulnetz;
CREATE DATABASE schulnetz;
USE schulnetz;

CREATE TABLE Department (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    Designation VARCHAR(100) NOT NULL
);

CREATE TABLE VicePrincipal (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    DepartmentId INT NOT NULL,
    Firstname VARCHAR(50) NOT NULL,
    Lastname VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,

    FOREIGN KEY (DepartmentId) REFERENCES Department(Id)
);

CREATE TABLE Class (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    Designation VARCHAR(10) NOT NULL
);

CREATE TABLE Student (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    ClassId INT NOT NULL,
    Firstname VARCHAR(50) NOT NULL,
    Lastname VARCHAR(50) NOT NULL,

    FOREIGN KEY (ClassId) REFERENCES Class(Id)
);

CREATE TABLE Specialisation (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    Designation VARCHAR(100) NOT NULL
);

CREATE TABLE Teacher (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    VicePrincipalId INT NOT NULL,
    SecondaryVicePrincipal INT, -- Default if teacher doesnt have a second vice principal is null
    SpecialisationId INT NOT NULL,
    Firstname VARCHAR(50) NOT NULL,
    Lastname VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,

    FOREIGN KEY (VicePrincipalId) REFERENCES VicePrincipal(Id),
    FOREIGN KEY (SpecialisationId) REFERENCES Specialisation(Id)
);

-- Is called Subject but references Modules aswell
CREATE TABLE `Subject` (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    Designation VARCHAR(20) NOT NULL
);

CREATE TABLE SubjectExecution (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    TeacherId INT NOT NULL,
    SubjectId INT NOT NULL,
    `Year` INT,

    FOREIGN KEY (TeacherId) REFERENCES Teacher(Id),
    FOREIGN KEY (SubjectId) REFERENCES `Subject`(Id)
);

CREATE TABLE Exam (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    SubjectExecutionId INT NOT NULL,
    Designation VARCHAR(100) NOT NULL,
    `Description` VARCHAR(255),
    `Weight` DOUBLE NOT NULL,

    FOREIGN KEY (SubjectExecutionId) REFERENCES SubjectExecution(Id)
);

CREATE TABLE SubjectAttendance (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    SubjectExecutionId INT NOT NULL,
    StudentId INT NOT NULL,
    ActualAverage DOUBLE,
    AdjustedAverage DOUBLE,

    FOREIGN KEY (SubjectExecutionId) REFERENCES SubjectExecution(Id),
    FOREIGN KEY (StudentId) REFERENCES Student(Id)
);

CREATE TABLE Grade (
    Id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    ExamId INT NOT NULL,
    SubjectAttendanceId INT NOT NULL,
    Grade DOUBLE NOT NULL,
    DateOfCompletion DATE NOT NULL,
    MissedDateCount INT NOT NULL, -- Default for changed grade is 1

    FOREIGN KEY (ExamId) REFERENCES Exam(Id),
    FOREIGN KEY (SubjectAttendanceId) REFERENCES SubjectAttendance(Id)
);
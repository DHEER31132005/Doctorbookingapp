DROP DATABASE IF EXISTS DoctorAppointmentDB;
CREATE DATABASE DoctorAppointmentDB;
USE DoctorAppointmentDB;

-- ==========================================
-- USERS TABLE (Modified)
-- ==========================================
CREATE TABLE Users
(
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    Role ENUM('Admin','Patient') NOT NULL DEFAULT 'Patient',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PasswordResetToken VARCHAR(255),
    ResetTokenExpiry DATETIME
);

-- ==========================================
-- SPECIALTIES TABLE
-- ==========================================
CREATE TABLE Specialties
(
    SpecialtyId INT AUTO_INCREMENT PRIMARY KEY,
    SpecialtyName VARCHAR(100) NOT NULL UNIQUE
);

-- ==========================================
-- DOCTORS TABLE
-- ==========================================
CREATE TABLE Doctors
(
    DoctorId INT AUTO_INCREMENT PRIMARY KEY,
    DoctorName VARCHAR(100) NOT NULL,
    SpecialtyId INT NOT NULL,
    ConsultationMode ENUM('Online','Offline') NOT NULL,
    ConsultationFee DECIMAL(10,2) NOT NULL,
    ExperienceYears INT DEFAULT 0,
    Email VARCHAR(100),
    PhoneNumber VARCHAR(20),
    ClinicAddress VARCHAR(255),
    Status ENUM('Active','Inactive') DEFAULT 'Active',

    FOREIGN KEY (SpecialtyId)
    REFERENCES Specialties(SpecialtyId)
);

-- ==========================================
-- DOCTOR AUTH TABLE (New)
-- ==========================================
CREATE TABLE DoctorAuth (
    DoctorAuthId INT AUTO_INCREMENT PRIMARY KEY,
    DoctorId INT NOT NULL UNIQUE,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    SecretKey VARCHAR(50) NOT NULL UNIQUE,
    PasswordResetToken VARCHAR(255),
    ResetTokenExpiry DATETIME,
    FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId) ON DELETE CASCADE
);

-- ==========================================
-- DOCTOR SLOTS
-- ==========================================
CREATE TABLE DoctorSlots
(
    SlotId INT AUTO_INCREMENT PRIMARY KEY,
    DoctorId INT NOT NULL,
    SlotDate DATE NOT NULL,
    SlotStartTime TIME NOT NULL,
    SlotEndTime TIME NOT NULL,
    IsBooked BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (DoctorId)
    REFERENCES Doctors(DoctorId) ON DELETE CASCADE
);

-- ==========================================
-- APPOINTMENTS TABLE
-- ==========================================
CREATE TABLE Appointments
(
    AppointmentId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    DoctorId INT NOT NULL,
    SlotId INT NOT NULL,
    AppointmentMode ENUM('Online','Offline') NOT NULL,
    Status ENUM('Confirmed','Completed','Cancelled','NoShow') DEFAULT 'Confirmed',
    BookingDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    AppointmentDate DATE NOT NULL,
    VideoLink VARCHAR(255),
    ClinicAddress VARCHAR(255),
    Notes TEXT,

    FOREIGN KEY(UserId) REFERENCES Users(UserId),
    FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId),
    FOREIGN KEY(SlotId) REFERENCES DoctorSlots(SlotId)
);

-- ==========================================
-- FEEDBACK TABLE (New)
-- ==========================================
CREATE TABLE Feedback (
    FeedbackId INT AUTO_INCREMENT PRIMARY KEY,
    AppointmentId INT NOT NULL,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    Comments TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(AppointmentId)
);

-- ==========================================
-- INSERT SPECIALTIES
-- ==========================================
INSERT INTO Specialties (SpecialtyName) VALUES
('General Physician'), ('Pediatrics'), ('Dermatology'), ('Gynecology'), 
('Orthopedics'), ('Cardiology'), ('Neurology'), ('Ophthalmology'), 
('ENT'), ('Psychiatry'), ('Psychology'), ('Gastroenterology'), 
('Nephrology'), ('Urology'), ('Oncology'), ('Rheumatology'), 
('Dentistry'), ('Physiotherapy'), ('Nutrition');

-- ==========================================
-- SEED DATA - Use hashed passwords in reality, these are raw for testing
-- The application uses BCrypt. 
-- The password '123456' hashes to: $2a$11$D.Zq2h4gDqO0B.N8a24tE.G6O8T4t1G/5c2.G1oV3P9.O7U1/5.Ue
-- We will use this hash so you can login with '123456'
-- ==========================================
SET @hash123456 = '$2a$11$D.Zq2h4gDqO0B.N8a24tE.G6O8T4t1G/5c2.G1oV3P9.O7U1/5.Ue';

INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role) VALUES
('System Admin', 'admin@hospital.com', @hash123456, '9999999999', 'Admin'),
('Gokul Krishnan', 'gokul@gmail.com', @hash123456, '9876500000', 'Patient'),
('Kumar', 'kumar@gmail.com', @hash123456, '9876500001', 'Patient'),
('Ravi', 'ravi@gmail.com', @hash123456, '9876500002', 'Patient'),
('Patient One', 'patient1@gmail.com', @hash123456, '8888888888', 'Patient'),
('Patient Two', 'patient2@gmail.com', @hash123456, '7777777777', 'Patient');

INSERT INTO Doctors (DoctorName, SpecialtyId, ConsultationMode, ConsultationFee, ExperienceYears, Email, PhoneNumber, ClinicAddress) VALUES
('Dr. Arjun Kumar',1,'Online',500,10,'arjun@gmail.com','9876543210','Chennai'),
('Dr. Priya Sharma',2,'Offline',700,8,'priya@gmail.com','9876543211','Coimbatore'),
('Dr. Rahul Singh',3,'Online',600,12,'rahul@gmail.com','9876543212','Madurai'),
('Dr. Sneha Reddy',4,'Offline',800,15,'sneha@gmail.com','9876543213','Salem'),
('Dr. Vikram Patel',5,'Online',750,11,'vikram@gmail.com','9876543214','Trichy'),
('Dr. Meena Devi',6,'Offline',900,18,'meena@gmail.com','9876543215','Erode'),
('Dr. Karthik Raj',7,'Online',850,14,'karthik@gmail.com','9876543216','Karur'),
('Dr. Deepa Nair',8,'Offline',650,9,'deepa@gmail.com','9876543217','Namakkal');

-- DOCTOR AUTH DATA
-- We will use '123456' for password and 'SEC123' as secret key for all doctors for ease of testing
INSERT INTO DoctorAuth (DoctorId, Email, PasswordHash, SecretKey) VALUES
(1, 'arjun@gmail.com', @hash123456, 'SEC123'),
(2, 'priya@gmail.com', @hash123456, 'SEC123'),
(3, 'rahul@gmail.com', @hash123456, 'SEC123'),
(4, 'sneha@gmail.com', @hash123456, 'SEC123'),
(5, 'vikram@gmail.com', @hash123456, 'SEC123'),
(6, 'meena@gmail.com', @hash123456, 'SEC123'),
(7, 'karthik@gmail.com', @hash123456, 'SEC123'),
(8, 'deepa@gmail.com', @hash123456, 'SEC123');

INSERT INTO DoctorSlots (DoctorId, SlotDate, SlotStartTime, SlotEndTime, IsBooked) VALUES
(1,CURDATE() + INTERVAL 1 DAY,'09:00:00','09:30:00',0),
(1,CURDATE() + INTERVAL 1 DAY,'09:30:00','10:00:00',0),
(1,CURDATE() + INTERVAL 1 DAY,'10:00:00','10:30:00',0),
(2,CURDATE() + INTERVAL 1 DAY,'11:00:00','11:30:00',0),
(2,CURDATE() + INTERVAL 1 DAY,'11:30:00','12:00:00',0),
(3,CURDATE() + INTERVAL 1 DAY,'14:00:00','14:30:00',0),
(3,CURDATE() + INTERVAL 1 DAY,'14:30:00','15:00:00',0),
(4,CURDATE() + INTERVAL 1 DAY,'16:00:00','16:30:00',0),
(4,CURDATE() + INTERVAL 1 DAY,'16:30:00','17:00:00',0);

-- APPOINTMENT SUMMARY VIEW
CREATE VIEW AppointmentSummary AS
SELECT a.AppointmentId, u.FullName, d.DoctorName, s.SpecialtyName, a.AppointmentMode, a.Status, a.AppointmentDate
FROM Appointments a
INNER JOIN Users u ON a.UserId=u.UserId
INNER JOIN Doctors d ON a.DoctorId=d.DoctorId
INNER JOIN Specialties s ON d.SpecialtyId=s.SpecialtyId;

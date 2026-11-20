create database RaceDayDB; 
Go
use RaceDayDB;
Go

create table Organiser(
OrganiserID integer identity(1,1) not null,
Name varchar(50),
Password varchar(255),
primary key (OrganiserID));

create table Participant(
ParticipantID integer identity(1,1) not null,
Name varchar(50),
PhoneNumber varchar(10),
Age integer,
Password varchar(255),
primary key (ParticipantID));

create table Category(
CategoryID integer identity(1,1) not null,
Name varchar(50),
OrganiserID integer not null,
primary key(CategoryID),
foreign key(OrganiserID) references Organiser(OrganiserID));

create table Location(
LocationID integer identity(1,1) not null,
Name varchar(50),
GPS_coordinates varchar(20),
StartingLineLocation varchar(20),
FinishLineLocation varchar(20),
RouteLength decimal(10,2),
OrganiserID integer not null,
primary key(LocationID),
foreign key (OrganiserID) references Organiser(OrganiserID));

create table Event(
EventID integer identity(1,1) not null,
Title varchar(50),
Date date not null,
StartTime time not null,
EndTime time not null,
CategoryID integer not null,
LocationID integer not null,
OrganiserID integer not null,
primary key(EventID),
foreign key(CategoryID) references Category(CategoryID),
foreign key(LocationID) references Location(LocationID),
foreign key(OrganiserID) references Organiser(OrganiserID));

create table Enrolment(
EnrolmentID integer identity(1,1) not null,
ParticipantID integer not null,
EventID integer not null,
primary key(EnrolmentID),
foreign key (ParticipantID) references Participant(ParticipantID),
foreign key (EventID) references Event(EventID));

create table Results(
ResultID integer identity(1,1) not null,
FinishingTime time,
FinishingPosition integer,
ParticipantID integer not null,
EventID integer not null,
OrganiserID integer not null,
primary key (ResultID),
foreign key (ParticipantID) references Participant(ParticipantID),
foreign key (EventID) references Event(EventID),
foreign key(OrganiserID) references Organiser(OrganiserID));
Go

INSERT INTO Organiser (Name, Password) 
VALUES 
  ('Sarah Jenkins', '$2a$11$eImiTXuWVxfM37uY4JANjO.g5aL5Y/Xf0R2O8/x8L3n9J2w4y6Z2G'), 
  ('David Naidoo',  '$2a$11$8kLq9Xw2M4pP1vNz3R5Y6e.u0I1O2P3Q4R5S6T7U8V9W0X1Y2Z3A4');

INSERT INTO Participant (Name, PhoneNumber, Age, Password) 
VALUES 
  ('Sipho Ndlovu',  '0821234567', 28, '$2a$11$eImiTXuWVxfM37uY4JANjO.g5aL5Y/Xf0R2O8/x8L3n9J2w4y6Z2G'), 
  ('Lerato Molefe', '0719876543', 22, '$2a$11$8kLq9Xw2M4pP1vNz3R5Y6e.u0I1O2P3Q4R5S6T7U8V9W0X1Y2Z3A4');

INSERT INTO Category (Name, OrganiserID) 
VALUES 
  ('Senior 10km', 1), 
  ('Veterans 21km', 1), 
  ('Fun Walk 5km', 2);

INSERT INTO Location (Name, GPS_coordinates, StartingLineLocation, FinishLineLocation, RouteLength, OrganiserID) 
VALUES 
  ('Durban Beachfront', '-29.8587, 31.0218', 'North Pier',  'South Beach', 10.00, 1),
  ('Cape Town CBD',     '-33.9249, 18.4241', 'Green Point', 'Stadium',     21.00, 1),
  ('Soweto Oval',       '-26.2485, 27.9016', 'Main Gate',   'Main Gate',   5.00,  2);

INSERT INTO Event (Title, Date, StartTime, EndTime, CategoryID, LocationID, OrganiserID) 
VALUES 
  ('Durban Beach 10k',          '2026-11-15', '06:00:00', '10:00:00', 1, 1, 1),
  ('Mother City Half Marathon', '2026-12-01', '06:30:00', '11:00:00', 2, 2, 1),
  ('Soweto Community Walk',     '2026-12-10', '08:00:00', '12:00:00', 3, 3, 2);

INSERT INTO Enrolment (ParticipantID, EventID) 
VALUES 
  (1, 1), 
  (2, 1), 
  (1, 2);

INSERT INTO Results (FinishingTime, FinishingPosition, ParticipantID, EventID, OrganiserID) 
VALUES 
  ('00:42:15', 1, 1, 1, 1),
  ('00:48:30', 2, 2, 1, 1);
Go
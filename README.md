# OpenHands-Volunteering-Platform (Final Year Project)

# Project Overview
OpenHands is a web-based volunteer management platform that connects volunteers with non-profit organizations. The system addresses the communication chaos and information loss present in traditional volunteer coordination by providing a structured digital solution. The platform includes three user modules: Volunteer, Organization, and Admin, with unique features such as conflict detection, credit-based trust scoring, emergency opportunities, and automated certificate generation.

# Team Details
- **Team Members**:
  - Teh Feng Yue
  - Teh Wen Wen
  - Angel Lee Ni
  - Woon Kai-En
  - Krithikaa Kumar

# Technology Stack
Framework: ASP.NET Core MVC (.NET 8.0)
Database: Microsoft SQL Server
IDE: Visual Studio 2022 
Database Management: SQL Server Management Studio (SSMS)
Frontend: Bootstrap 5, Bootstrap Icons, JavaScript, HTML/CSS
Authentication: ASP.NET Core Identity 
Email Service: SMTP (Gmail)

# Prerequisites
Before running this project, ensure you have installed the following:
1. Visual Studio 2022 (Community Edition or higher)
   - With "ASP.NET and web development" workload
   - With .NET 8.0 SDK
2. SQL Server Management Studio 20

# Installation Guide
## First - Open the Project
1. Launch Visual Studio 2022
2. Click 'Open a project or solution'
3. Navigate to the project folder and select the `OpenHands.sln` file

## Second - Configure Database Connection
Open `appsettings.json` in the root directory. Locate the connection string and change the server to your SQL Server instance name:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME\\SQLEXPRESS;Database=OpenHandsDB;Trusted_Connection=true;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

## Third - Apply Database Migrations
In Visual Studio:
1. Go to **Tools → NuGet Package Manager → Package Manager Console**
2. Ensure the default project is set to the main project
3. Run the following command:

```bash
update-database
```

If you encounter migration errors, delete the `Migrations` folder and run:

```bash
add-migration InitialCreate
update-database
```

## Fourth - Run the Application
Press **F5** or click **"Debug → Start Debugging"** in Visual Studio. The application will launch in your default web browser at `https://localhost:5001` or `http://localhost:5000`.

---

## Demo Accounts
The following accounts are available for testing different user roles:
### ADMIN ACCOUNT
email: admin@openhands.com 
password: Admin@123
*Admin can verify organizations, lock/unlock accounts, manage reports, and view platform analytics.*

### ORGANIZATION ACCOUNTS (Approved)
*Organization accounts can create opportunities, manage volunteers, mark attendance, and generate certificates.*

### ORGANIZATION ACCOUNT (Pending Verification)
*Cannot create opportunity until approved by admin.*

### VOLUNTEER ACCOUNTS
*Volunteer accounts can browse opportunities, apply for events, follow organizations, and earn credit scores.*

---

## How to Register New Accounts
### Register as a Volunteer
1. Click **"Register"** on the homepage
2. Select **"Create Volunteer Account"**
3. Fill in the registration form (name, email, password, date of birth, phone number)
4. Click **"Submit"**
5. You will be automatically logged in

### Register as an Organization
1. Click **"Register"** on the homepage
2. Select **"Create Organization Account"**
3. Fill in the registration form (organization name, email, password, phone number, mission statement)
4. Upload organization logo (PNG/JPG, max 5MB)
5. Upload registration certificate (PDF, max 15MB)
6. Click **"Submit"**
7. Wait for admin approval email

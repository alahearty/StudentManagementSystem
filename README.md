# Student Management System

A complete academic administration desktop application built with **.NET 8, WPF (MVVM), Entity Framework Core and PostgreSQL**.

## Features

| Module | Description |
|---|---|
| **Authentication** | Login with PBKDF2 password hashing, roles (Admin/Teacher), role-based UI access |
| **Students** | CRUD, server-side search & pagination, validation (email/phone/age), CSV import |
| **Courses** | CRUD, prerequisite courses enforced at enrollment time |
| **Enrollments** | Enroll/remove, date-range & grade-status filters, duplicate protection |
| **Results** | Result computation engine: CA (40) + Exam (60) = Total (100) → letter grade → grade point, draft/publish workflow |
| **Transcripts** | Per-student transcript: scores, GPA per semester, CGPA, class of degree, PDF export |
| **Attendance** | Mark per course/date (Present/Absent/Late/Excused), history, PDF report |
| **Timetable** | Weekly schedules with rooms and instructors, day filter |
| **Payments** | Record payments, totals, PDF report |
| **Semesters** | Semester entity management with date ranges (admin) |
| **Users** | User management with password reset (admin) |
| **Dashboard** | Stats cards, students-by-department chart, CSV/PDF export, sample data seeding |
| **Notifications** | In-app notification center for success/warning events |
| **Logging** | Serilog console + rolling file logs |

## Result Computation Engine

- **Scoring:** Continuous Assessment (max 40) + Exam (max 60) = Total (max 100)
- **Grade bands:** A+ (90+) → F (< 35) with grade points (4.0 → 0.0)
- **GPA:** credit-weighted, computed per semester and cumulative
- **Classification:** First Class Honours (≥ 3.6) → Pass
- **Publication:** results start as Draft and are locked once Published

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) running on `localhost:5432`

## Setup

1. Update the connection string in `appsettings.json` if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=StudentManagementDb;Username=postgres;Password=YOUR_PASSWORD"
   }
   ```
2. Run the app — the database is created automatically on first launch (EF Core migrations).
3. Sign in with the default admin account:
   - **Username:** `admin`
   - **Password:** `admin123`

## Build & Test

```powershell
dotnet build
dotnet test Tests\StudentManagementSystem.Tests.csproj
```

## Project Structure

```
├── Commands/         # RelayCommand / AsyncRelayCommand
├── Converters/       # XAML value converters
├── Data/             # AppDbContext, design-time factory
├── Migrations/       # EF Core migrations
├── Models/           # Entities (Student, Course, Enrollment, Semester, Attendance, Schedule, Payment, User)
├── Services/         # Auth, ResultComputationEngine, PdfExporter, CsvImporter, DataSeeder, NotificationCenter, LogConfig
├── Themes/           # ModernTheme.xaml
├── ViewModels/       # MVVM view models
├── Views/            # WPF windows (Login, Main, forms, dialogs)
└── Tests/            # xUnit test project
```

## Tech Stack

.NET 8 · WPF · MVVM · Entity Framework Core 8 · PostgreSQL (Npgsql) · Serilog · QuestPDF · xUnit

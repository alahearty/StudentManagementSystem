using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services;

public static class DataSeeder
{
    public static void Seed(IDbContextFactory<AppDbContext> contextFactory)
    {
        using var context = contextFactory.CreateDbContext();

        if (context.Users.Any(u => u.Username == "admin"))
            return;

        context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = AuthService.HashPassword("admin123"),
            Role = "Admin",
            DisplayName = "System Administrator",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        context.SaveChanges();
    }

    public static (int Students, int Courses, int Enrollments, int Semesters, int Schedules, int Payments) SeedSampleData(
        IDbContextFactory<AppDbContext> contextFactory)
    {
        using var context = contextFactory.CreateDbContext();

        if (context.Students.Any() && context.Courses.Any())
            return (0, 0, 0, 0, 0, 0);

        var semesters = new List<Semester>
        {
            new() { Name = "2024-Fall", StartDate = Utc(2024, 9, 1), EndDate = Utc(2024, 12, 20), IsActive = true },
            new() { Name = "2025-Spring", StartDate = Utc(2025, 1, 15), EndDate = Utc(2025, 5, 10), IsActive = true }
        };
        context.Semesters.AddRange(semesters);

        var courses = new List<Course>
        {
            new() { CourseCode = "CS101", CourseName = "Introduction to Programming", Credits = 3, Department = "Computer Science", Description = "Fundamentals of programming with C#." },
            new() { CourseCode = "CS201", CourseName = "Data Structures", Credits = 4, Department = "Computer Science", Description = "Lists, trees, graphs and algorithms.", PrerequisiteCourseId = null },
            new() { CourseCode = "CS301", CourseName = "Database Systems", Credits = 3, Department = "Computer Science", Description = "Relational databases and SQL." },
            new() { CourseCode = "MA101", CourseName = "Calculus I", Credits = 4, Department = "Mathematics", Description = "Limits, derivatives and integrals." },
            new() { CourseCode = "PH101", CourseName = "Physics Fundamentals", Credits = 3, Department = "Physics", Description = "Mechanics and thermodynamics." },
            new() { CourseCode = "EN101", CourseName = "Academic Writing", Credits = 2, Department = "English", Description = "Essay writing and research skills." }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        if (courses[1].Id != 0)
        {
            var cs201 = courses[1];
            cs201.PrerequisiteCourseId = courses[0].Id;
        }
        context.SaveChanges();

        var students = new List<Student>
        {
            new() { RegistrationNumber = "STU-0001", FirstName = "Alice", LastName = "Johnson", Department = "Computer Science", Email = "alice.j@example.com", Phone = "+1234567890", Gender = "Female", DateOfBirth = Utc(2004, 3, 15), EnrollmentDate = Utc(2024, 9, 1) },
            new() { RegistrationNumber = "STU-0002", FirstName = "Bob", LastName = "Smith", Department = "Computer Science", Email = "bob.s@example.com", Phone = "+1234567891", Gender = "Male", DateOfBirth = Utc(2003, 7, 22), EnrollmentDate = Utc(2024, 9, 1) },
            new() { RegistrationNumber = "STU-0003", FirstName = "Carol", LastName = "Williams", Department = "Mathematics", Email = "carol.w@example.com", Phone = "+1234567892", Gender = "Female", DateOfBirth = Utc(2004, 11, 2), EnrollmentDate = Utc(2024, 9, 1) },
            new() { RegistrationNumber = "STU-0004", FirstName = "David", LastName = "Brown", Department = "Physics", Email = "david.b@example.com", Phone = "+1234567893", Gender = "Male", DateOfBirth = Utc(2003, 1, 30), EnrollmentDate = Utc(2024, 9, 1) },
            new() { RegistrationNumber = "STU-0005", FirstName = "Eve", LastName = "Davis", Department = "Computer Science", Email = "eve.d@example.com", Phone = "+1234567894", Gender = "Female", DateOfBirth = Utc(2005, 5, 18), EnrollmentDate = Utc(2025, 1, 15) }
        };
        context.Students.AddRange(students);
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Semester = "2024-Fall", Grade = "A", CaScore = 34, ExamScore = 52 },
            new() { StudentId = students[0].Id, CourseId = courses[3].Id, Semester = "2024-Fall", Grade = "B+", CaScore = 30, ExamScore = 43 },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Semester = "2024-Fall", Grade = "B", CaScore = 28, ExamScore = 40 },
            new() { StudentId = students[1].Id, CourseId = courses[1].Id, Semester = "2025-Spring", CaScore = 25, ExamScore = 38 },
            new() { StudentId = students[2].Id, CourseId = courses[3].Id, Semester = "2024-Fall", Grade = "A-", CaScore = 32, ExamScore = 46 },
            new() { StudentId = students[3].Id, CourseId = courses[4].Id, Semester = "2024-Fall", Grade = "C+", CaScore = 22, ExamScore = 34 },
            new() { StudentId = students[4].Id, CourseId = courses[0].Id, Semester = "2025-Spring", CaScore = 26, ExamScore = 39 }
        };
        context.Enrollments.AddRange(enrollments);

        var schedules = new List<Schedule>
        {
            new() { CourseId = courses[0].Id, DayOfWeek = "Monday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "A101", Instructor = "Dr. Adams" },
            new() { CourseId = courses[1].Id, DayOfWeek = "Wednesday", StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "B202", Instructor = "Prof. Baker" },
            new() { CourseId = courses[3].Id, DayOfWeek = "Tuesday", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "C303", Instructor = "Dr. Chen" }
        };
        context.Schedules.AddRange(schedules);

        var payments = new List<Payment>
        {
            new() { StudentId = students[0].Id, Amount = 1500m, PaymentDate = Utc(2024, 9, 5), PaymentMethod = "Bank Transfer", Description = "Tuition - 2024-Fall", Semester = "2024-Fall" },
            new() { StudentId = students[1].Id, Amount = 1500m, PaymentDate = Utc(2024, 9, 6), PaymentMethod = "Cash", Description = "Tuition - 2024-Fall", Semester = "2024-Fall" },
            new() { StudentId = students[2].Id, Amount = 1200m, PaymentDate = Utc(2024, 9, 8), PaymentMethod = "Online", Description = "Tuition - 2024-Fall", Semester = "2024-Fall" }
        };
        context.Payments.AddRange(payments);

        context.SaveChanges();

        return (students.Count, courses.Count, enrollments.Count, semesters.Count, schedules.Count, payments.Count);
    }

    private static DateTime Utc(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}

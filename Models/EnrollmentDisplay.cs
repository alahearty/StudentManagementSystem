namespace StudentManagementSystem.Models;

public sealed class EnrollmentDisplay
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public string Semester { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}

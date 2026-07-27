namespace StudentManagementSystem.Models;

public sealed class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string? Grade { get; set; }
    public string Semester { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

namespace StudentManagementSystem.Models;

public sealed class Course
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public string? Department { get; set; }
    public int? PrerequisiteCourseId { get; set; }
    public Course? PrerequisiteCourse { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

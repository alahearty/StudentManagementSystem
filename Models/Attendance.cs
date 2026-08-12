namespace StudentManagementSystem.Models;

public sealed class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Present";
    public string? Remarks { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

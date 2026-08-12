using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models;

public sealed class Schedule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Room { get; set; }
    public string? Instructor { get; set; }

    public Course Course { get; set; } = null!;

    [NotMapped]
    public string TimeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
}

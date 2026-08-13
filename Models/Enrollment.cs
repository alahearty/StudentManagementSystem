using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models;

public sealed class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string? Grade { get; set; }
    public string Semester { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public decimal? CaScore { get; set; }
    public decimal? ExamScore { get; set; }
    public bool IsResultPublished { get; set; }
    public DateTime? ResultPublishedAt { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;

    [NotMapped]
    public decimal? TotalScore => CaScore.HasValue && ExamScore.HasValue ? CaScore + ExamScore : null;

    [NotMapped]
    public decimal? GradePoint => Services.ResultComputationEngine.GradePointFromGrade(Grade);
}

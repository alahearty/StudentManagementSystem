namespace StudentManagementSystem.Services;

public static class ResultComputationEngine
{
    public const decimal CaMaxScore = 40m;
    public const decimal ExamMaxScore = 60m;
    public const decimal TotalMaxScore = CaMaxScore + ExamMaxScore;

    public static readonly IReadOnlyList<GradeBand> GradeBands = new List<GradeBand>
    {
        new("A+", 90m, 100m, 4.0m, "Outstanding"),
        new("A", 80m, 89.99m, 4.0m, "Excellent"),
        new("A-", 75m, 79.99m, 3.7m, "Very Good"),
        new("B+", 70m, 74.99m, 3.3m, "Good"),
        new("B", 65m, 69.99m, 3.0m, "Good"),
        new("B-", 60m, 64.99m, 2.7m, "Fairly Good"),
        new("C+", 55m, 59.99m, 2.3m, "Fair"),
        new("C", 50m, 54.99m, 2.0m, "Fair"),
        new("C-", 45m, 49.99m, 1.7m, "Pass"),
        new("D+", 40m, 44.99m, 1.3m, "Bare Pass"),
        new("D", 35m, 39.99m, 1.0m, "Marginal Pass"),
        new("F", 0m, 34.99m, 0.0m, "Fail")
    };

    public static decimal? ComputeTotalScore(decimal? caScore, decimal? examScore)
    {
        if (!caScore.HasValue || !examScore.HasValue) return null;
        return caScore.Value + examScore.Value;
    }

    public static string? ComputeGrade(decimal? totalScore)
    {
        if (!totalScore.HasValue) return null;
        foreach (var band in GradeBands)
        {
            if (totalScore.Value >= band.MinScore && totalScore.Value <= band.MaxScore)
                return band.Grade;
        }
        return "F";
    }

    public static string? ComputeGrade(decimal? caScore, decimal? examScore)
    {
        return ComputeGrade(ComputeTotalScore(caScore, examScore));
    }

    public static decimal? GradePointFromGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;
        var band = GradeBands.FirstOrDefault(b => b.Grade == grade.Trim().ToUpper());
        return band?.GradePoint;
    }

    public static bool IsPassingGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return false;
        var band = GradeBands.FirstOrDefault(b => b.Grade == grade.Trim().ToUpper());
        return band is not null && band.GradePoint > 0;
    }

    public static decimal ComputeGpa(IEnumerable<(string? Grade, int Credits)> gradedEnrollments)
    {
        decimal totalPoints = 0;
        int totalCredits = 0;

        foreach (var (grade, credits) in gradedEnrollments)
        {
            var gp = GradePointFromGrade(grade);
            if (gp.HasValue)
            {
                totalPoints += gp.Value * credits;
                totalCredits += credits;
            }
        }

        return totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;
    }

    public static decimal ComputeGpa(IEnumerable<(string? Grade, int Credits, string Semester)> enrollments)
    {
        return ComputeGpa(enrollments
            .Where(e => !string.IsNullOrWhiteSpace(e.Grade))
            .Select(e => (e.Grade, e.Credits)));
    }

    public static List<(string Semester, decimal Gpa, int Courses, int Credits, int Passes, int Fails)> GetSemesterBreakdown(
        IEnumerable<(string? Grade, int Credits, string Semester)> enrollments)
    {
        return enrollments
            .GroupBy(e => e.Semester)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var graded = g.Where(e => !string.IsNullOrWhiteSpace(e.Grade)).ToList();
                var gpa = ComputeGpa(graded.Select(e => (e.Grade, e.Credits)));
                return (
                    Semester: g.Key,
                    Gpa: gpa,
                    Courses: graded.Count,
                    Credits: graded.Sum(e => e.Credits),
                    Passes: graded.Count(e => IsPassingGrade(e.Grade)),
                    Fails: graded.Count(e => !IsPassingGrade(e.Grade))
                );
            })
            .ToList();
    }

    public static string GetClassOfDegree(decimal gpa)
    {
        return gpa switch
        {
            >= 3.6m => "First Class Honours",
            >= 3.0m => "Second Class Honours (Upper Division)",
            >= 2.0m => "Second Class Honours (Lower Division)",
            >= 1.0m => "Third Class Honours",
            >= 0.01m => "Pass",
            _ => "No Classification"
        };
    }

    public static string GetGradeRemark(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return "No Grade";
        var band = GradeBands.FirstOrDefault(b => b.Grade == grade.Trim().ToUpper());
        return band?.Remark ?? "Unknown";
    }

    public static bool ValidateScores(decimal? caScore, decimal? examScore, out string? error)
    {
        error = null;
        if (!caScore.HasValue && !examScore.HasValue) return true;
        if (!caScore.HasValue || !examScore.HasValue)
        {
            error = "Both CA and Exam scores must be entered together.";
            return false;
        }
        if (caScore.Value < 0 || caScore.Value > CaMaxScore)
        {
            error = $"CA score must be between 0 and {CaMaxScore}.";
            return false;
        }
        if (examScore.Value < 0 || examScore.Value > ExamMaxScore)
        {
            error = $"Exam score must be between 0 and {ExamMaxScore}.";
            return false;
        }
        return true;
    }
}

public sealed record GradeBand(string Grade, decimal MinScore, decimal MaxScore, decimal GradePoint, string Remark);

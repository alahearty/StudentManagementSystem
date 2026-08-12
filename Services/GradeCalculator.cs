namespace StudentManagementSystem.Services;

public static class GradeCalculator
{
    private static readonly Dictionary<string, double> GradePoints = new()
    {
        ["A+"] = 4.0, ["A"] = 4.0, ["A-"] = 3.7,
        ["B+"] = 3.3, ["B"] = 3.0, ["B-"] = 2.7,
        ["C+"] = 2.3, ["C"] = 2.0, ["C-"] = 1.7,
        ["D+"] = 1.3, ["D"] = 1.0, ["D-"] = 0.7,
        ["F"]  = 0.0
    };

    public static double GetGradePoint(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return 0;
        return GradePoints.TryGetValue(grade.Trim().ToUpper(), out var gp) ? gp : 0;
    }

    public static (double SemesterGpa, double CumulativeGpa) CalculateGpa(
        List<(string? Grade, int Credits, string Semester)> enrollments)
    {
        var semesters = enrollments
            .Where(e => !string.IsNullOrWhiteSpace(e.Grade))
            .GroupBy(e => e.Semester)
            .OrderBy(g => g.Key);

        double totalPoints = 0;
        double totalCredits = 0;

        foreach (var semester in semesters)
        {
            double semPoints = 0;
            double semCredits = 0;

            foreach (var enrollment in semester)
            {
                var gp = GetGradePoint(enrollment.Grade);
                semPoints += gp * enrollment.Credits;
                semCredits += enrollment.Credits;
            }

            totalPoints += semPoints;
            totalCredits += semCredits;
        }

        return totalCredits > 0
            ? (0, Math.Round(totalPoints / totalCredits, 2))
            : (0, 0);
    }

    public static List<(string Semester, double Gpa, int Courses, int Credits)> GetSemesterBreakdown(
        List<(string? Grade, int Credits, string Semester)> enrollments)
    {
        return enrollments
            .Where(e => !string.IsNullOrWhiteSpace(e.Grade))
            .GroupBy(e => e.Semester)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                double points = 0;
                int creds = 0;
                foreach (var e in g)
                {
                    points += GetGradePoint(e.Grade) * e.Credits;
                    creds += e.Credits;
                }
                var gpa = creds > 0 ? Math.Round(points / creds, 2) : 0;
                return (Semester: g.Key, Gpa: gpa, Courses: g.Count(), Credits: creds);
            })
            .ToList();
    }
}

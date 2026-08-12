using Xunit;

namespace StudentManagementSystem.Tests;

public class GradeCalculatorTests
{
    [Fact]
    public void GetGradePoint_ValidGrades_ReturnsCorrectPoints()
    {
        Assert.Equal(4.0, GradeCalculator.GetGradePoint("A+"));
        Assert.Equal(4.0, GradeCalculator.GetGradePoint("A"));
        Assert.Equal(3.7, GradeCalculator.GetGradePoint("A-"));
        Assert.Equal(3.0, GradeCalculator.GetGradePoint("B"));
        Assert.Equal(2.0, GradeCalculator.GetGradePoint("C"));
        Assert.Equal(1.0, GradeCalculator.GetGradePoint("D"));
        Assert.Equal(0.0, GradeCalculator.GetGradePoint("F"));
    }

    [Fact]
    public void GetGradePoint_NullOrEmpty_ReturnsZero()
    {
        Assert.Equal(0, GradeCalculator.GetGradePoint(null));
        Assert.Equal(0, GradeCalculator.GetGradePoint(""));
        Assert.Equal(0, GradeCalculator.GetGradePoint("  "));
    }

    [Fact]
    public void CalculateGpa_SingleSemester_ComputesCorrectly()
    {
        var enrollments = new List<(string?, int, string)>
        {
            ("A", 3, "2024-Fall"),
            ("B", 3, "2024-Fall"),
            ("A", 4, "2024-Fall")
        };

        var (_, cumulative) = GradeCalculator.CalculateGpa(enrollments);

        Assert.Equal(3.7, cumulative);
    }

    [Fact]
    public void CalculateGpa_MultipleSemesters_ComputesCumulativeGpa()
    {
        var enrollments = new List<(string?, int, string)>
        {
            ("A", 3, "2024-Fall"),
            ("B+", 4, "2024-Fall"),
            ("A-", 3, "2025-Spring"),
            ("C", 3, "2025-Spring")
        };

        var (_, cumulative) = GradeCalculator.CalculateGpa(enrollments);
        // Fall: (4.0*3 + 3.3*4) = 12 + 13.2 = 25.2 / 7
        // Spring: (3.7*3 + 2.0*3) = 11.1 + 6 = 17.1 / 6
        // Total: (25.2 + 17.1) / 13 = 42.3 / 13 ≈ 3.25
        Assert.Equal(3.25, cumulative);
    }

    [Fact]
    public void CalculateGpa_NoGrades_ReturnsZero()
    {
        var enrollments = new List<(string?, int, string)>
        {
            (null, 3, "2024-Fall"),
            (null, 4, "2024-Fall")
        };

        var (_, cumulative) = GradeCalculator.CalculateGpa(enrollments);
        Assert.Equal(0, cumulative);
    }

    [Fact]
    public void GetSemesterBreakdown_ReturnsCorrectGroups()
    {
        var enrollments = new List<(string?, int, string)>
        {
            ("A", 3, "2024-Fall"),
            ("B", 4, "2024-Fall"),
            ("A-", 3, "2025-Spring")
        };

        var breakdown = GradeCalculator.GetSemesterBreakdown(enrollments);

        Assert.Equal(2, breakdown.Count);
        Assert.Equal("2024-Fall", breakdown[0].Semester);
        Assert.Equal(2, breakdown[0].Courses);
        Assert.Equal(7, breakdown[0].Credits);
    }
}

using Xunit;

namespace StudentManagementSystem.Tests;

public class ResultComputationEngineTests
{
    [Theory]
    [InlineData(35, 55, 90, "A+")]
    [InlineData(32, 50, 82, "A")]
    [InlineData(30, 47, 77, "A-")]
    [InlineData(28, 44, 72, "B+")]
    [InlineData(26, 41, 67, "B")]
    [InlineData(24, 38, 62, "B-")]
    [InlineData(22, 35, 57, "C+")]
    [InlineData(20, 32, 52, "C")]
    [InlineData(18, 29, 47, "C-")]
    [InlineData(16, 26, 42, "D+")]
    [InlineData(14, 23, 37, "D")]
    [InlineData(10, 20, 30, "F")]
    [InlineData(0, 0, 0, "F")]
    public void ComputeGrade_WithScores_ReturnsCorrectGrade(decimal ca, decimal exam, decimal expectedTotal, string expectedGrade)
    {
        var total = ResultComputationEngine.ComputeTotalScore(ca, exam);
        Assert.Equal(expectedTotal, total);
        Assert.Equal(expectedGrade, ResultComputationEngine.ComputeGrade(ca, exam));
    }

    [Fact]
    public void ComputeGrade_WithNoScores_ReturnsNull()
    {
        Assert.Null(ResultComputationEngine.ComputeGrade(null, null));
        Assert.Null(ResultComputationEngine.ComputeGrade(30m, null));
        Assert.Null(ResultComputationEngine.ComputeGrade(null, 40m));
    }

    [Fact]
    public void ComputeTotalScore_WithNoScores_ReturnsNull()
    {
        Assert.Null(ResultComputationEngine.ComputeTotalScore(null, null));
        Assert.Null(ResultComputationEngine.ComputeTotalScore(30m, null));
    }

    public static TheoryData<string?, decimal?> GradePointData => new()
    {
        { "A+", 4.0m },
        { "A", 4.0m },
        { "A-", 3.7m },
        { "B+", 3.3m },
        { "B", 3.0m },
        { "C", 2.0m },
        { "D", 1.0m },
        { "F", 0.0m },
        { null, null },
        { "", null }
    };

    [Theory]
    [MemberData(nameof(GradePointData))]
    public void GradePointFromGrade_ReturnsCorrectPoints(string? grade, decimal? expected)
    {
        Assert.Equal(expected, ResultComputationEngine.GradePointFromGrade(grade));
    }

    [Fact]
    public void ComputeGpa_WeightedByCredits_ComputesCorrectly()
    {
        var enrollments = new List<(string?, int)>
        {
            ("A", 3),
            ("B", 4),
            ("C", 3)
        };
        // (4.0*3 + 3.0*4 + 2.0*3) / 10 = (12 + 12 + 6) / 10 = 3.0
        Assert.Equal(3.0m, ResultComputationEngine.ComputeGpa(enrollments));
    }

    [Fact]
    public void ComputeGpa_NoGrades_ReturnsZero()
    {
        var enrollments = new List<(string?, int)>
        {
            (null, 3),
            (null, 4)
        };
        Assert.Equal(0m, ResultComputationEngine.ComputeGpa(enrollments));
    }

    [Theory]
    [InlineData(3.7, "First Class Honours")]
    [InlineData(3.2, "Second Class Honours (Upper Division)")]
    [InlineData(2.5, "Second Class Honours (Lower Division)")]
    [InlineData(1.5, "Third Class Honours")]
    [InlineData(0.5, "Pass")]
    [InlineData(0, "No Classification")]
    public void GetClassOfDegree_ReturnsCorrectClassification(decimal gpa, string expected)
    {
        Assert.Equal(expected, ResultComputationEngine.GetClassOfDegree(gpa));
    }

    [Theory]
    [InlineData(30, 40, true)]
    [InlineData(-5, 40, false)]
    [InlineData(45, 40, false)]
    [InlineData(30, 65, false)]
    [InlineData(30, -1, false)]
    public void ValidateScores_ChecksRanges(decimal ca, decimal exam, bool expectedValid)
    {
        Assert.Equal(expectedValid, ResultComputationEngine.ValidateScores(ca, exam, out _));
    }

    [Fact]
    public void ValidateScores_PartialScores_ReturnsError()
    {
        Assert.False(ResultComputationEngine.ValidateScores(30m, null, out var error));
        Assert.Contains("together", error);
    }

    [Fact]
    public void IsPassingGrade_FailGrades_ReturnFalse()
    {
        Assert.False(ResultComputationEngine.IsPassingGrade("F"));
        Assert.False(ResultComputationEngine.IsPassingGrade(null));
        Assert.False(ResultComputationEngine.IsPassingGrade(""));
        Assert.True(ResultComputationEngine.IsPassingGrade("D"));
        Assert.True(ResultComputationEngine.IsPassingGrade("A"));
    }

    [Fact]
    public void GetGradeRemark_ReturnsRemark()
    {
        Assert.Equal("Excellent", ResultComputationEngine.GetGradeRemark("A"));
        Assert.Equal("Fail", ResultComputationEngine.GetGradeRemark("F"));
        Assert.Equal("No Grade", ResultComputationEngine.GetGradeRemark(null));
    }
}

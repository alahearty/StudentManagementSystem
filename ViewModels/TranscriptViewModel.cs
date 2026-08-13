using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels.Base;

namespace StudentManagementSystem.ViewModels;

public sealed class TranscriptViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly int _studentId;

    public TranscriptViewModel(IDbContextFactory<AppDbContext> contextFactory, Student student)
    {
        _contextFactory = contextFactory;
        _studentId = student.Id;
        StudentName = student.FullName;
        RegistrationNumber = student.RegistrationNumber;
        Department = student.Department;
        LoadCommand = new AsyncRelayCommand(async _ => await LoadTranscriptAsync());
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
    }

    public ICommand LoadCommand { get; }
    public ICommand CloseCommand { get; }

    public string StudentName { get; }
    public string RegistrationNumber { get; }
    public string Department { get; }

    private double _cumulativeGpa;
    public double CumulativeGpa
    {
        get => _cumulativeGpa;
        set => SetProperty(ref _cumulativeGpa, value);
    }
    private int _totalCredits;
    public int TotalCredits
    {
        get => _totalCredits;
        set => SetProperty(ref _totalCredits, value);
    }

    private int _totalCourses;
    public int TotalCourses
    {
        get => _totalCourses;
        set => SetProperty(ref _totalCourses, value);
    }

    private string _honors = string.Empty;
    public string Honors
    {
        get => _honors;
        set => SetProperty(ref _honors, value);
    }

    public ObservableCollection<TranscriptEntry> Entries { get; } = new();
    public ObservableCollection<SemesterSummary> SemesterSummaries { get; } = new();

    public Action? CloseRequested { get; set; }

    private async Task LoadTranscriptAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var enrollments = await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == _studentId)
            .OrderBy(e => e.Semester).ThenBy(e => e.Course.CourseCode)
            .Select(e => new
            {
                e.Course.CourseCode,
                e.Course.CourseName,
                e.Course.Credits,
                e.Grade,
                e.Semester,
                e.EnrollmentDate,
                e.CaScore,
                e.ExamScore
            })
            .ToListAsync();

        Entries.Clear();
        foreach (var e in enrollments)
        {
            Entries.Add(new TranscriptEntry
            {
                CourseCode = e.CourseCode,
                CourseName = e.CourseName,
                Credits = e.Credits,
                Grade = e.Grade ?? "-",
                Semester = e.Semester,
                EnrollmentDate = e.EnrollmentDate,
                CaScore = e.CaScore,
                ExamScore = e.ExamScore,
                TotalScore = ResultComputationEngine.ComputeTotalScore(e.CaScore, e.ExamScore),
                GradePoint = ResultComputationEngine.GradePointFromGrade(e.Grade)
            });
        }

        var gpaInput = enrollments.Select(e => (e.Grade, e.Credits, e.Semester)).ToList();
        var cumulative = ResultComputationEngine.ComputeGpa(gpaInput);
        CumulativeGpa = (double)cumulative;

        var graded = enrollments.Where(e => !string.IsNullOrWhiteSpace(e.Grade)).ToList();
        TotalCourses = graded.Count;
        TotalCredits = graded.Sum(e => e.Credits);
        Honors = GetHonors(cumulative);

        SemesterSummaries.Clear();
        foreach (var s in ResultComputationEngine.GetSemesterBreakdown(gpaInput))
        {
            SemesterSummaries.Add(new SemesterSummary
            {
                Semester = s.Semester,
                Gpa = (double)s.Gpa,
                Courses = s.Courses,
                Credits = s.Credits,
                Passes = s.Passes,
                Fails = s.Fails
            });
        }
    }

    private static string GetHonors(decimal gpa)
    {
        return ResultComputationEngine.GetClassOfDegree(gpa);
    }
}

public sealed class TranscriptEntry
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string Grade { get; set; } = "-";
    public string Semester { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public decimal? CaScore { get; set; }
    public decimal? ExamScore { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? GradePoint { get; set; }
}

public sealed class SemesterSummary
{
    public string Semester { get; set; } = string.Empty;
    public double Gpa { get; set; }
    public int Courses { get; set; }
    public int Credits { get; set; }
    public int Passes { get; set; }
    public int Fails { get; set; }
}

using System.Text;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;

namespace StudentManagementSystem.Services;

public static class CsvExporter
{
    public static async Task<string> ExportStudentsAsync(IDbContextFactory<AppDbContext> contextFactory)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var students = await context.Students.OrderBy(s => s.RegistrationNumber).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("RegistrationNumber,FirstName,LastName,Department,Email,Phone,Gender,DateOfBirth,EnrollmentDate,Address");
        foreach (var s in students)
        {
            sb.AppendLine(EscapeCsv(new[]
            {
                s.RegistrationNumber, s.FirstName, s.LastName, s.Department, s.Email,
                s.Phone ?? "", s.Gender ?? "",
                s.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                s.EnrollmentDate?.ToString("yyyy-MM-dd") ?? "",
                s.Address ?? ""
            }));
        }
        return sb.ToString();
    }

    public static async Task<string> ExportEnrollmentsAsync(IDbContextFactory<AppDbContext> contextFactory)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var enrollments = await context.Enrollments
            .Include(e => e.Student).Include(e => e.Course)
            .OrderBy(e => e.Semester).ThenBy(e => e.Student.LastName)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("StudentName,RegistrationNumber,CourseCode,CourseName,Semester,Grade,EnrollmentDate");
        foreach (var e in enrollments)
        {
            sb.AppendLine(EscapeCsv(new[]
            {
                e.Student.FullName, e.Student.RegistrationNumber,
                e.Course.CourseCode, e.Course.CourseName,
                e.Semester, e.Grade ?? "", e.EnrollmentDate.ToString("yyyy-MM-dd")
            }));
        }
        return sb.ToString();
    }

    public static void SaveFile(string content, string defaultFileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() == true)
        {
            System.IO.File.WriteAllText(dialog.FileName, content);
            System.Windows.MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private static string EscapeCsv(string[] fields)
    {
        return string.Join(",", fields.Select(f =>
        {
            if (f.Contains(',') || f.Contains('"') || f.Contains('\n'))
                return $"\"{f.Replace("\"", "\"\"")}\"";
            return f;
        }));
    }
}

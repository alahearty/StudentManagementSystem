using System.IO;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services;

public static class CsvImporter
{
    public static async Task<(int Imported, int Skipped, List<string> Errors)> ImportStudentsAsync(
        IDbContextFactory<AppDbContext> factory, string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length < 2)
            return (0, 0, new List<string> { "File is empty or missing header row." });

        var errors = new List<string>();
        var imported = 0;
        var skipped = 0;

        await using var context = await factory.CreateDbContextAsync();

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 5)
            {
                errors.Add($"Row {i + 1}: Not enough columns.");
                skipped++;
                continue;
            }

            var regNo = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(regNo))
            {
                errors.Add($"Row {i + 1}: Missing registration number.");
                skipped++;
                continue;
            }

            if (await context.Students.AnyAsync(s => s.RegistrationNumber == regNo))
            {
                skipped++;
                continue;
            }

            try
            {
                DateTime? dob = null;
                if (parts.Length > 6 && DateTime.TryParse(parts[6], out var parsedDob))
                    dob = DateTime.SpecifyKind(parsedDob, DateTimeKind.Utc);

                DateTime? enrollDate = null;
                if (parts.Length > 9 && DateTime.TryParse(parts[9], out var parsedEnroll))
                    enrollDate = DateTime.SpecifyKind(parsedEnroll, DateTimeKind.Utc);

                context.Students.Add(new Student
                {
                    RegistrationNumber = regNo,
                    FirstName = parts.Length > 1 ? parts[1].Trim() : "",
                    LastName = parts.Length > 2 ? parts[2].Trim() : "",
                    Department = parts.Length > 3 ? parts[3].Trim() : "",
                    Email = parts.Length > 4 ? parts[4].Trim() : "",
                    Phone = parts.Length > 5 ? parts[5].Trim() : null,
                    Gender = parts.Length > 7 ? parts[7].Trim() : null,
                    Address = parts.Length > 8 ? parts[8].Trim() : null,
                    DateOfBirth = dob,
                    EnrollmentDate = enrollDate
                });
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {i + 1}: {ex.Message}");
                skipped++;
            }
        }

        await context.SaveChangesAsync();
        return (imported, skipped, errors);
    }

    public static async Task<(int Imported, int Skipped, List<string> Errors)> ImportCoursesAsync(
        IDbContextFactory<AppDbContext> factory, string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length < 2)
            return (0, 0, new List<string> { "File is empty or missing header row." });

        var errors = new List<string>();
        var imported = 0;
        var skipped = 0;

        await using var context = await factory.CreateDbContextAsync();

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length < 3)
            {
                errors.Add($"Row {i + 1}: Not enough columns.");
                skipped++;
                continue;
            }

            var code = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add($"Row {i + 1}: Missing course code.");
                skipped++;
                continue;
            }

            if (await context.Courses.AnyAsync(c => c.CourseCode == code))
            {
                skipped++;
                continue;
            }

            try
            {
                int.TryParse(parts.Length > 2 ? parts[2].Trim() : "3", out var credits);
                context.Courses.Add(new Course
                {
                    CourseCode = code,
                    CourseName = parts.Length > 1 ? parts[1].Trim() : "",
                    Credits = credits > 0 ? credits : 3,
                    Description = parts.Length > 3 ? parts[3].Trim() : null,
                    Department = parts.Length > 4 ? parts[4].Trim() : null
                });
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {i + 1}: {ex.Message}");
                skipped++;
            }
        }

        await context.SaveChangesAsync();
        return (imported, skipped, errors);
    }

    public static (int, int, List<string>) OpenAndImport(
        Func<IDbContextFactory<AppDbContext>, string, Task<(int, int, List<string>)>> importFunc,
        IDbContextFactory<AppDbContext> factory,
        string fileType)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = $"Import {fileType} from CSV"
        };

        if (dialog.ShowDialog() != true)
            return (0, 0, new List<string>());

        var result = importFunc(factory, dialog.FileName).GetAwaiter().GetResult();

        var msg = $"Import complete.\n\nImported: {result.Item1}\nSkipped: {result.Item2}";
        if (result.Item3.Count > 0)
        {
            msg += $"\n\nErrors ({result.Item3.Count}):\n" + string.Join("\n", result.Item3.Take(10));
            if (result.Item3.Count > 10)
                msg += $"\n... and {result.Item3.Count - 10} more.";
        }

        System.Windows.MessageBox.Show(msg, "Import Results",
            System.Windows.MessageBoxButton.OK,
            result.Item3.Count > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);

        return (result.Item1, result.Item2, result.Item3);
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim('"'));
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current.Trim('"'));
        return result.ToArray();
    }
}

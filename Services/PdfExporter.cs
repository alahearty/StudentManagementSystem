using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentManagementSystem.Data;

namespace StudentManagementSystem.Services;

public static class PdfExporter
{
    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static async Task<byte[]> ExportTranscriptAsync(IDbContextFactory<AppDbContext> factory, int studentId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var student = await context.Students.FindAsync(studentId);
        if (student is null) return Array.Empty<byte>();

        var enrollments = await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Semester).ThenBy(e => e.Course.CourseCode)
            .ToListAsync();

        var gpaInput = enrollments.Select(e => (e.Grade, e.Course.Credits, e.Semester)).ToList();
        var cumulative = ResultComputationEngine.ComputeGpa(gpaInput);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => c.Column(col =>
                {
                    col.Item().Text("STUDENT TRANSCRIPT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Student: {student.FullName}").FontSize(13).Bold();
                    col.Item().Text($"Reg No: {student.RegistrationNumber}  |  Dept: {student.Department}");
                    col.Item().Text($"CGPA: {cumulative:F2}  |  {ResultComputationEngine.GetClassOfDegree(cumulative)}").FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                }));

                page.Content().Element(c => c.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(90);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Course Code").Bold();
                        header.Cell().Text("Course Name").Bold();
                        header.Cell().Text("Credits").Bold();
                        header.Cell().Text("Grade").Bold();
                        header.Cell().Text("Semester").Bold();
                    });

                    foreach (var e in enrollments)
                    {
                        table.Cell().Text(e.Course.CourseCode);
                        table.Cell().Text(e.Course.CourseName);
                        table.Cell().Text(e.Course.Credits.ToString());
                        table.Cell().Text(e.Grade ?? "-");
                        table.Cell().Text(e.Semester);
                    }
                }));

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated: ");
                    x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        }).GeneratePdf();
    }

    public static async Task<byte[]> ExportAttendanceReportAsync(IDbContextFactory<AppDbContext> factory, int courseId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var course = await context.Courses.FindAsync(courseId);
        var records = await context.Attendances
            .Include(a => a.Student)
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.Date).ThenBy(a => a.Student.LastName)
            .ToListAsync();

        var totalStudents = records.Select(r => r.StudentId).Distinct().Count();
        var presentCount = records.Count(r => r.Status == "Present");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => c.Column(col =>
                {
                    col.Item().Text("ATTENDANCE REPORT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Course: {course?.CourseName ?? "N/A"} ({course?.CourseCode})").FontSize(13).Bold();
                    col.Item().Text($"Total Students: {totalStudents}  |  Total Records: {records.Count}  |  Present: {presentCount}");
                }));

                page.Content().Element(c => c.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Student").Bold();
                        header.Cell().Text("Status").Bold();
                        header.Cell().Text("Date").Bold();
                        header.Cell().Text("Remarks").Bold();
                    });

                    foreach (var r in records)
                    {
                        table.Cell().Text(r.Student.FullName);
                        table.Cell().Text(r.Status);
                        table.Cell().Text(r.Date.ToString("yyyy-MM-dd"));
                        table.Cell().Text(r.Remarks ?? "");
                    }
                }));
            });
        }).GeneratePdf();
    }

    public static async Task<byte[]> ExportPaymentsReportAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();
        var payments = await context.Payments
            .Include(p => p.Student)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        var total = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => c.Column(col =>
                {
                    col.Item().Text("PAYMENT REPORT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Total Collected: ${total:N2}").FontSize(14).Bold();
                }));

                page.Content().Element(c => c.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Student").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Method").Bold();
                        header.Cell().Text("Status").Bold();
                        header.Cell().Text("Date").Bold();
                    });

                    foreach (var p in payments)
                    {
                        table.Cell().Text(p.Student.FullName);
                        table.Cell().Text($"${p.Amount:N2}");
                        table.Cell().Text(p.PaymentMethod);
                        table.Cell().Text(p.Status);
                        table.Cell().Text(p.PaymentDate.ToString("yyyy-MM-dd"));
                    }
                }));
            });
        }).GeneratePdf();
    }

    public static void SavePdf(byte[] pdf, string defaultFileName)
    {
        if (pdf.Length == 0) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() == true)
        {
            System.IO.File.WriteAllBytes(dialog.FileName, pdf);
            System.Windows.MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}

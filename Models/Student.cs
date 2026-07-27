using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models;

public sealed class Student
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? EnrollmentDate { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

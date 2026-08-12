namespace StudentManagementSystem.Models;

public sealed class Payment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Status { get; set; } = "Completed";
    public string? Semester { get; set; }

    public Student Student { get; set; } = null!;
}

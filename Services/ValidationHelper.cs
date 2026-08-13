using System.Text.RegularExpressions;

namespace StudentManagementSystem.Services;

public static partial class ValidationHelper
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[\+\d][\d\s\-\(\)]{6,19}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[A-Za-z0-9\-_\s]+$")]
    private static partial Regex AlphanumericRegex();

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex().IsMatch(email.Trim());
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        return PhoneRegex().IsMatch(phone.Trim());
    }

    public static bool IsValidAge(DateTime? dateOfBirth, int minAge = 10, int maxAge = 120)
    {
        if (!dateOfBirth.HasValue) return true;
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;
        return age >= minAge && age <= maxAge;
    }

    public static bool IsNotInFuture(DateTime? date)
    {
        if (!date.HasValue) return true;
        return date.Value.Date <= DateTime.Today;
    }

    public static bool IsValidGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return true;
        return ResultComputationEngine.GradePointFromGrade(grade) is not null;
    }

    public static bool IsValidCredits(int credits, int min = 1, int max = 12)
    {
        return credits >= min && credits <= max;
    }
}

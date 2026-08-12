using Xunit;

namespace StudentManagementSystem.Tests;

public class ValidationHelperTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co", true)]
    [InlineData("a@b.c", true)]
    [InlineData("notanemail", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@", false)]
    public void IsValidEmail_ValidatesCorrectly(string? email, bool expected)
    {
        Assert.Equal(expected, ValidationHelper.IsValidEmail(email));
    }

    [Theory]
    [InlineData("+1234567890", true)]
    [InlineData("123-456-7890", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("abc", false)]
    public void IsValidPhone_ValidatesCorrectly(string? phone, bool expected)
    {
        Assert.Equal(expected, ValidationHelper.IsValidPhone(phone));
    }

    [Fact]
    public void IsValidAge_WithinRange_ReturnsTrue()
    {
        Assert.True(ValidationHelper.IsValidAge(DateTime.Today.AddYears(-20)));
        Assert.True(ValidationHelper.IsValidAge(DateTime.Today.AddYears(-100)));
    }

    [Fact]
    public void IsValidAge_OutsideRange_ReturnsFalse()
    {
        Assert.False(ValidationHelper.IsValidAge(DateTime.Today.AddYears(-5)));
        Assert.False(ValidationHelper.IsValidAge(DateTime.Today.AddYears(-130)));
    }

    [Fact]
    public void IsNotInFuture_FutureDate_ReturnsFalse()
    {
        Assert.False(ValidationHelper.IsNotInFuture(DateTime.Today.AddDays(1)));
    }

    [Fact]
    public void IsNotInFuture_PastOrToday_ReturnsTrue()
    {
        Assert.True(ValidationHelper.IsNotInFuture(DateTime.Today));
        Assert.True(ValidationHelper.IsNotInFuture(DateTime.Today.AddDays(-1)));
        Assert.True(ValidationHelper.IsNotInFuture(null));
    }

    [Theory]
    [InlineData("A+", true)]
    [InlineData("A", true)]
    [InlineData("B-", true)]
    [InlineData("F", true)]
    [InlineData("E", false)]
    [InlineData("X", false)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void IsValidGrade_ValidatesCorrectly(string? grade, bool expected)
    {
        Assert.Equal(expected, ValidationHelper.IsValidGrade(grade));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(0, false)]
    [InlineData(13, false)]
    [InlineData(-1, false)]
    public void IsValidCredits_ValidatesCorrectly(int credits, bool expected)
    {
        Assert.Equal(expected, ValidationHelper.IsValidCredits(credits));
    }
}

using Xunit;

namespace StudentManagementSystem.Tests;

public class AuthServiceTests
{
    [Fact]
    public void HashPassword_GeneratesValidHash()
    {
        var hash = AuthService.HashPassword("test123");
        Assert.NotNull(hash);
        Assert.Contains(".", hash);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hash = AuthService.HashPassword("correct-horse-battery-staple");
        Assert.True(AuthService.VerifyPassword("correct-horse-battery-staple", hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = AuthService.HashPassword("admin123");
        Assert.False(AuthService.VerifyPassword("wrong", hash));
    }

    [Fact]
    public void HashPassword_GeneratesUniqueHashes()
    {
        var hash1 = AuthService.HashPassword("password");
        var hash2 = AuthService.HashPassword("password");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        Assert.False(AuthService.VerifyPassword("test", "not-a-valid-hash"));
        Assert.False(AuthService.VerifyPassword("test", ""));
    }
}

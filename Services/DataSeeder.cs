using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services;

public static class DataSeeder
{
    public static void Seed(IDbContextFactory<AppDbContext> contextFactory)
    {
        using var context = contextFactory.CreateDbContext();

        if (context.Users.Any(u => u.Username == "admin"))
            return;

        context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = AuthService.HashPassword("admin123"),
            Role = "Admin",
            DisplayName = "System Administrator",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        context.SaveChanges();
    }
}

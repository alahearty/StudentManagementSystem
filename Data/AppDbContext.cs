using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(s => s.RegistrationNumber).IsUnique();
            entity.Property(s => s.RegistrationNumber).HasMaxLength(20).IsRequired();
            entity.Property(s => s.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(s => s.LastName).HasMaxLength(50).IsRequired();
            entity.Property(s => s.Department).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Gender).HasMaxLength(15);
            entity.Property(s => s.Phone).HasMaxLength(20);
            entity.Property(s => s.Address).HasMaxLength(200);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.CourseCode).IsUnique();
            entity.Property(c => c.CourseCode).HasMaxLength(20).IsRequired();
            entity.Property(c => c.CourseName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.Department).HasMaxLength(100);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Enrollments)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Semester }).IsUnique();
            entity.Property(e => e.Grade).HasMaxLength(5);
            entity.Property(e => e.Semester).HasMaxLength(20).IsRequired();
        });
    }
}

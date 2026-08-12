using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Payment> Payments => Set<Payment>();

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

            entity.HasOne(c => c.PrerequisiteCourse)
                  .WithMany()
                  .HasForeignKey(c => c.PrerequisiteCourseId)
                  .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Course)
                  .WithMany()
                  .HasForeignKey(a => a.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => new { a.StudentId, a.CourseId, a.Date }).IsUnique();
            entity.Property(a => a.Status).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Remarks).HasMaxLength(200);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasOne(s => s.Course)
                  .WithMany()
                  .HasForeignKey(s => s.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.DayOfWeek).HasMaxLength(15).IsRequired();
            entity.Property(s => s.Room).HasMaxLength(50);
            entity.Property(s => s.Instructor).HasMaxLength(100);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Student)
                  .WithMany()
                  .HasForeignKey(p => p.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Description).HasMaxLength(200);
            entity.Property(p => p.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(p => p.Status).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Semester).HasMaxLength(20);
        });
    }
}

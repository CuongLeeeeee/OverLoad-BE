using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.Implementations;

namespace OverLoad.Tests;

/// <summary>
/// A small, fixed data set shared by unit and integration tests:
/// three users (one per role), a free course, a paid course and a draft course.
/// </summary>
public static class TestData
{
    public const string Password = "Passw0rd!";

    public static AppDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static async Task<Seeded> SeedAsync(AppDbContext context)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4);
        var admin = new User { Email = "admin@test.io", FullName = "Admin", Role = UserRole.Admin, PasswordHash = hash };
        var instructor = new User { Email = "instructor@test.io", FullName = "Instructor", Role = UserRole.Instructor, PasswordHash = hash };
        var student = new User { Email = "student@test.io", FullName = "Student", Role = UserRole.Student, PasswordHash = hash };

        var freeCourse = new Course { Title = "Free course", Slug = "free-course", Status = CourseStatus.Published, Price = 0, InstructorId = 1 };
        var paidCourse = new Course { Title = "Paid course", Slug = "paid-course", Status = CourseStatus.Published, Price = 199000, InstructorId = 1 };
        var draftCourse = new Course { Title = "Draft course", Slug = "draft-course", Status = CourseStatus.Draft, Price = 0, InstructorId = 1 };

        var paidLessons = new[]
        {
            new Lesson { Course = paidCourse, Title = "Intro (free preview)", OrderIndex = 1, IsFree = true, DurationMinutes = 10, Content = "intro" },
            new Lesson { Course = paidCourse, Title = "Part 2", OrderIndex = 2, IsFree = false, DurationMinutes = 20, Content = "secret-2" },
            new Lesson { Course = paidCourse, Title = "Part 3", OrderIndex = 3, IsFree = false, DurationMinutes = 30, Content = "secret-3" },
            new Lesson { Course = paidCourse, Title = "Part 4", OrderIndex = 4, IsFree = false, DurationMinutes = 40, Content = "secret-4" }
        };
        var freeLesson = new Lesson { Course = freeCourse, Title = "Free lesson", OrderIndex = 1, IsFree = true, DurationMinutes = 5 };
        var draftLesson = new Lesson { Course = draftCourse, Title = "Draft lesson", OrderIndex = 1, IsFree = true };

        context.Users.AddRange(admin, instructor, student);
        context.Courses.AddRange(freeCourse, paidCourse, draftCourse);
        context.Lessons.AddRange(paidLessons);
        context.Lessons.AddRange(freeLesson, draftLesson);
        await context.SaveChangesAsync();

        return new Seeded(admin, instructor, student, freeCourse, paidCourse, draftCourse, paidLessons, freeLesson, draftLesson);
    }

    public static CourseAccessService CourseAccess(AppDbContext context)
        => new(new EnrollmentRepository(context), new UserRepository(context));

    public static async Task MakeProAsync(AppDbContext context, User user, DateTime? expiresAt = null)
    {
        user.ProExpiresAt = expiresAt ?? DateTime.UtcNow.AddMonths(1);
        await context.SaveChangesAsync();
    }

    public static async Task EnrollAsync(AppDbContext context, User user, Course course)
    {
        context.Enrollments.Add(new Enrollment { UserId = user.Id, CourseId = course.Id });
        await context.SaveChangesAsync();
    }

    public record Seeded(
        User Admin,
        User Instructor,
        User Student,
        Course FreeCourse,
        Course PaidCourse,
        Course DraftCourse,
        Lesson[] PaidCourseLessons,
        Lesson FreeLesson,
        Lesson DraftLesson);
}

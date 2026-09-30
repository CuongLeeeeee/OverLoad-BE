using Microsoft.EntityFrameworkCore;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using Xunit;

namespace OverLoad.Tests.Services;

public class ProgressServiceTests
{
    private static ProgressService CreateService(AppDbContext context) => new(
        new UserLessonProgressRepository(context),
        new LessonRepository(context),
        new EnrollmentRepository(context),
        TestData.CourseAccess(context));

    [Fact]
    public async Task CompleteLessonAsync_Should_Recalculate_Course_Progress_On_The_Server()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context);

        await service.CompleteLessonAsync(data.Student.Id, data.PaidCourseLessons[0].Id);
        await service.CompleteLessonAsync(data.Student.Id, data.PaidCourseLessons[1].Id);

        var enrollment = await context.Enrollments.SingleAsync();
        Assert.Equal(50m, enrollment.ProgressPercentage);   // 2 of 4 lessons
        Assert.Null(enrollment.CompletedAt);
    }

    [Fact]
    public async Task CompleteLessonAsync_Should_Mark_Enrollment_Completed_At_100_Percent()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context);

        foreach (var lesson in data.PaidCourseLessons)
            await service.CompleteLessonAsync(data.Student.Id, lesson.Id);

        var enrollment = await context.Enrollments.SingleAsync();
        Assert.Equal(100m, enrollment.ProgressPercentage);
        Assert.NotNull(enrollment.CompletedAt);
    }

    [Fact]
    public async Task CompleteLessonAsync_Should_Be_Idempotent()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context);

        await service.CompleteLessonAsync(data.Student.Id, data.PaidCourseLessons[0].Id);
        await service.CompleteLessonAsync(data.Student.Id, data.PaidCourseLessons[0].Id);

        Assert.Single(context.UserLessonProgresses);
        Assert.Equal(25m, (await context.Enrollments.SingleAsync()).ProgressPercentage);
    }

    [Fact]
    public async Task CompleteLessonAsync_Should_Forbid_Paid_Lesson_When_Not_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService(context).CompleteLessonAsync(data.Student.Id, data.PaidCourseLessons[1].Id));
    }

    [Fact]
    public async Task SaveMyLessonProgressAsync_Should_Create_Then_Update_The_Same_Record()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context);
        var lessonId = data.PaidCourseLessons[1].Id;

        await service.SaveMyLessonProgressAsync(data.Student.Id, lessonId, new SaveProgressRequest { LastPositionSeconds = 30 });
        var result = await service.SaveMyLessonProgressAsync(data.Student.Id, lessonId, new SaveProgressRequest { LastPositionSeconds = 90 });

        Assert.Single(context.UserLessonProgresses);
        Assert.Equal(90, result.LastPositionSeconds);
        Assert.False(result.Completed);
    }

    [Fact]
    public async Task SaveMyLessonProgressAsync_Should_Not_Uncomplete_A_Completed_Lesson()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        var service = CreateService(context);
        var lessonId = data.PaidCourseLessons[1].Id;

        await service.SaveMyLessonProgressAsync(data.Student.Id, lessonId, new SaveProgressRequest { Completed = true });
        var result = await service.SaveMyLessonProgressAsync(data.Student.Id, lessonId, new SaveProgressRequest { Completed = false });

        Assert.True(result.Completed);
    }

    [Fact]
    public async Task SaveMyLessonProgressAsync_Should_Allow_Free_Lesson_Without_Enrollment()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context).SaveMyLessonProgressAsync(
            data.Student.Id, data.PaidCourseLessons[0].Id, new SaveProgressRequest { LastScrollPercentage = 40 });

        Assert.Equal(40m, result.LastScrollPercentage);
    }
}

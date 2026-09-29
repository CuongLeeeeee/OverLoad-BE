using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using Xunit;

namespace OverLoad.Tests.Services;

public class LessonServiceTests
{
    private static LessonService CreateService(AppDbContext context) => new(
        new LessonRepository(context),
        new CourseRepository(context),
        new UserLessonProgressRepository(context),
        new EnrollmentRepository(context));

    [Fact]
    public async Task GetByIdAsync_Should_Return_Free_Lesson_To_Anonymous()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context).GetByIdAsync(data.PaidCourseLessons[0].Id, userId: null, isStaff: false);

        Assert.Equal("intro", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Require_Sign_In_For_Paid_Lesson()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateService(context).GetByIdAsync(data.PaidCourseLessons[1].Id, userId: null, isStaff: false));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Forbid_Paid_Lesson_When_Not_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService(context).GetByIdAsync(data.PaidCourseLessons[1].Id, data.Student.Id, isStaff: false));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Paid_Lesson_When_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);

        var result = await CreateService(context).GetByIdAsync(data.PaidCourseLessons[1].Id, data.Student.Id, isStaff: false);

        Assert.Equal("secret-2", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Paid_Lesson_To_Staff()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context).GetByIdAsync(data.PaidCourseLessons[1].Id, data.Instructor.Id, isStaff: true);

        Assert.Equal("secret-2", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Hide_Lessons_Of_Draft_Course_From_Non_Staff()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService(context).GetByIdAsync(data.DraftLesson.Id, data.Student.Id, isStaff: false));
    }

    [Fact]
    public async Task GetByCourseIdWithProgressAsync_Should_Lock_Paid_Lessons_When_Not_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var lessons = await CreateService(context).GetByCourseIdWithProgressAsync(data.PaidCourse.Id, data.Student.Id, isStaff: false);

        Assert.False(lessons[0].IsLocked);                     // free preview
        Assert.All(lessons.Skip(1), l => Assert.True(l.IsLocked));
    }

    [Fact]
    public async Task GetByCourseIdWithProgressAsync_Should_Unlock_Next_Lesson_After_Previous_Is_Completed()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.PaidCourse);
        context.UserLessonProgresses.Add(new() { UserId = data.Student.Id, LessonId = data.PaidCourseLessons[0].Id, Completed = true });
        await context.SaveChangesAsync();

        var lessons = await CreateService(context).GetByCourseIdWithProgressAsync(data.PaidCourse.Id, data.Student.Id, isStaff: false);

        Assert.False(lessons[1].IsLocked);  // previous (lesson 1) completed
        Assert.True(lessons[2].IsLocked);   // lesson 2 not completed yet
    }
}

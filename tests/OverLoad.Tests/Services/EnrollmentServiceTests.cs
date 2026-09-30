using OverLoad.Repositories.Implementations;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using OverLoad.Repositories.Data;
using Xunit;

namespace OverLoad.Tests.Services;

public class EnrollmentServiceTests
{
    private static EnrollmentService CreateService(AppDbContext context) => new(
        new EnrollmentRepository(context),
        new UserRepository(context),
        new CourseRepository(context),
        new UserLessonProgressRepository(context),
        TestData.CourseAccess(context));

    [Fact]
    public async Task SelfEnrollAsync_Should_Enroll_In_Free_Published_Course()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context).SelfEnrollAsync(data.Student.Id, data.FreeCourse.Id);

        Assert.Equal(data.FreeCourse.Id, result.CourseId);
        Assert.Equal(data.Student.Id, result.UserId);
        Assert.Single(context.Enrollments);
    }

    [Fact]
    public async Task SelfEnrollAsync_Should_Reject_Paid_Course()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateService(context).SelfEnrollAsync(data.Student.Id, data.PaidCourse.Id));
        Assert.Empty(context.Enrollments);
    }

    [Fact]
    public async Task SelfEnrollAsync_Should_Hide_Draft_Course()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService(context).SelfEnrollAsync(data.Student.Id, data.DraftCourse.Id));
    }

    [Fact]
    public async Task SelfEnrollAsync_Should_Throw_Conflict_When_Already_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        await TestData.EnrollAsync(context, data.Student, data.FreeCourse);

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateService(context).SelfEnrollAsync(data.Student.Id, data.FreeCourse.Id));
    }

    [Fact]
    public async Task GrantAsync_Should_Allow_Paid_Course()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        var result = await CreateService(context).GrantAsync(
            new CreateEnrollmentRequest { UserId = data.Student.Id, CourseId = data.PaidCourse.Id });

        Assert.Equal(data.PaidCourse.Id, result.CourseId);
    }

    [Fact]
    public async Task GetCourseProgressAsync_Should_Throw_When_Not_Enrolled()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService(context).GetCourseProgressAsync(data.Student.Id, data.PaidCourse.Id));
    }
}

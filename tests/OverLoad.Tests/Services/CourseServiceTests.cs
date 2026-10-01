using OverLoad.Repositories.Implementations;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using Xunit;

namespace OverLoad.Tests.Services;

public class CourseServiceTests
{
    [Fact]
    public async Task GetAllAsync_Should_Only_Return_Published_Courses_To_Non_Staff_Even_When_Asked_For_Drafts()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);
        var service = new CourseService(new CourseRepository(context));

        var result = await service.GetAllAsync(new CourseQueryParams { IsPublished = false }, includeUnpublished: false);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, c => Assert.True(c.IsPublished));
    }

    [Fact]
    public async Task GetAllAsync_Should_Let_Staff_Filter_Drafts()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);
        var service = new CourseService(new CourseRepository(context));

        var result = await service.GetAllAsync(new CourseQueryParams { IsPublished = false }, includeUnpublished: true);

        Assert.Single(result.Items);
        Assert.Equal("draft-course", result.Items[0].Slug);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Hide_Draft_Course_From_Non_Staff()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var service = new CourseService(new CourseRepository(context));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(data.DraftCourse.Id, includeUnpublished: false));
        Assert.Equal("draft-course", (await service.GetByIdAsync(data.DraftCourse.Id, includeUnpublished: true)).Slug);
    }

    [Fact]
    public async Task CreateAsync_Should_Generate_Unique_Slug()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);
        var service = new CourseService(new CourseRepository(context));

        var created = await service.CreateAsync(new CreateCourseRequest { Title = "Free Course" }, instructorId: 1);

        Assert.StartsWith("free-course-", created.Slug);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Course_Does_Not_Exist()
    {
        await using var context = TestData.CreateContext();
        var service = new CourseService(new CourseRepository(context));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(999, new UpdateCourseRequest { Title = "x" }, callerId: 1, isAdmin: true));
    }
}

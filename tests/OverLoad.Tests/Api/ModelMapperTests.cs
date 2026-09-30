using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using Xunit;

namespace OverLoad.Tests.Api;

public class ModelMapperTests
{
    [Fact]
    public void Query_Mapping_Should_Keep_Service_Default_Sort_When_SortBy_Is_Omitted()
    {
        var enrollments = new EnrollmentQueryRequestModel { PageNumber = 2 }.ToServiceDto();
        var lessons = new LessonQueryRequestModel().ToServiceDto();

        Assert.Equal("-enrolledAt", enrollments.SortBy);
        Assert.Equal(2, enrollments.PageNumber);
        Assert.Equal("orderIndex", lessons.SortBy);
    }

    [Fact]
    public void Query_Mapping_Should_Apply_Clamping_And_Explicit_Sort()
    {
        var query = new CourseQueryRequestModel { PageSize = 1000, PageNumber = -3, SortBy = "title" }.ToServiceDto();

        Assert.Equal(100, query.PageSize);
        Assert.Equal(1, query.PageNumber);
        Assert.Equal("title", query.SortBy);
    }
}

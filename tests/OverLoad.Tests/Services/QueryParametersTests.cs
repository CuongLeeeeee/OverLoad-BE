using OverLoad.Services.DTOs.Request;
using Xunit;

namespace OverLoad.Tests.Services;

public class QueryParametersTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void PageNumber_Should_Be_At_Least_One(int input, int expected)
    {
        Assert.Equal(expected, new CourseQueryParams { PageNumber = input }.PageNumber);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(500, 100)]
    [InlineData(25, 25)]
    public void PageSize_Should_Be_Clamped(int input, int expected)
    {
        Assert.Equal(expected, new CourseQueryParams { PageSize = input }.PageSize);
    }

    [Theory]
    [InlineData("-createdAt", "createdat", true)]
    [InlineData("title", "title", false)]
    [InlineData(" -TotalLessons ", "totallessons", true)]
    public void ParseSort_Should_Split_Field_And_Direction(string sortBy, string field, bool descending)
    {
        Assert.Equal((field, descending), new CourseQueryParams { SortBy = sortBy }.ParseSort());
    }
}

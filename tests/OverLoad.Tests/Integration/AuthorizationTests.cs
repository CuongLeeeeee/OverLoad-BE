using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OverLoad.Tests.Integration;

/// <summary>Checks who may call what, end to end through the HTTP pipeline.</summary>
public class AuthorizationTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        if (_factory.Data == null)
            await _factory.InitializeDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("GET", "/api/v1/users")]
    [InlineData("GET", "/api/v1/me")]
    [InlineData("GET", "/api/v1/enrollments")]
    [InlineData("GET", "/api/v1/progress")]
    [InlineData("GET", "/api/v1/admin/revenue-stats")]
    [InlineData("GET", "/api/v1/admin/transactions")]
    [InlineData("POST", "/api/v1/courses/1/enroll")]
    [InlineData("POST", "/api/v1/chat")]
    [InlineData("GET", "/api/v1/me/wallet")]
    [InlineData("GET", "/api/v1/me/transactions")]
    [InlineData("POST", "/api/v1/me/wallet/purchase-pro")]
    [InlineData("POST", "/api/v1/payment/checkout")]
    [InlineData("GET", "/api/v1/payment/orders/1")]
    public async Task Protected_Endpoints_Should_Return_401_Envelope_For_Anonymous(string method, string url)
    {
        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("success").GetBoolean());
    }

    [Theory]
    [InlineData("GET", "/api/v1/users")]
    [InlineData("DELETE", "/api/v1/users/1")]
    [InlineData("GET", "/api/v1/enrollments")]
    [InlineData("GET", "/api/v1/progress")]
    [InlineData("GET", "/api/v1/lessons")]
    [InlineData("GET", "/api/v1/admin/revenue-stats")]
    [InlineData("GET", "/api/v1/admin/transactions")]
    [InlineData("DELETE", "/api/v1/courses/1")]
    public async Task Admin_Or_Staff_Endpoints_Should_Return_403_For_Student(string method, string url)
    {
        var client = _factory.CreateClientFor(_factory.Data.Student);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Instructor_Should_Not_Manage_Users_Or_Grant_Enrollments()
    {
        var client = _factory.CreateClientFor(_factory.Data.Instructor);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/enrollments",
            new { userId = _factory.Data.Student.Id, courseId = _factory.Data.PaidCourse.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/enrollments")).StatusCode);
    }

    [Fact]
    public async Task Admin_Should_List_Users_In_Paged_Envelope()
    {
        var client = _factory.CreateClientFor(_factory.Data.Admin);

        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/users?pageSize=2");

        Assert.True(body.GetProperty("success").GetBoolean());
        var data = body.GetProperty("data");
        Assert.Equal(2, data.GetProperty("items").GetArrayLength());
        Assert.Equal(3, data.GetProperty("totalCount").GetInt32());
        Assert.True(data.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task Admin_Cannot_Lock_Own_Account_And_Students_Cannot_Lock_Anyone()
    {
        var admin = _factory.CreateClientFor(_factory.Data.Admin);
        var student = _factory.CreateClientFor(_factory.Data.Student);

        var self = await admin.PostAsync($"/api/v1/users/{_factory.Data.Admin.Id}/lock", null);
        var byStudent = await student.PostAsync($"/api/v1/users/{_factory.Data.Instructor.Id}/lock", null);

        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byStudent.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Course_List_Should_Not_Include_Drafts()
    {
        var body = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/v1/courses?isPublished=false");

        var slugs = body.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(c => c.GetProperty("slug").GetString())
            .ToList();
        Assert.DoesNotContain("draft-course", slugs);
    }

    [Fact]
    public async Task Paid_Lesson_Content_Should_Require_Sign_In_And_Enrollment()
    {
        var lessonUrl = $"/api/v1/lessons/{_factory.Data.PaidCourseLessons[2].Id}";

        var anonymous = await _factory.CreateClient().GetAsync(lessonUrl);
        var student = await _factory.CreateClientFor(_factory.Data.Student).GetAsync(lessonUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
    }

    [Fact]
    public async Task Student_Should_Not_Self_Enroll_In_Paid_Course()
    {
        var client = _factory.CreateClientFor(_factory.Data.Student);

        var response = await client.PostAsync($"/api/v1/courses/{_factory.Data.PaidCourse.Id}/enroll", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Body_Should_Return_Validation_Envelope()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "1", fullName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation failed.", body.GetProperty("message").GetString());
        Assert.True(body.GetProperty("errors").GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Enums_Should_Be_Serialized_As_Names()
    {
        var body = await _factory.CreateClient().GetFromJsonAsync<JsonElement>(
            $"/api/v1/courses/{_factory.Data.FreeCourse.Id}");

        Assert.Equal("Beginner", body.GetProperty("data").GetProperty("level").GetString());
    }

    [Theory]
    [InlineData("/api/v1/payment/webhook")]
    [InlineData("/api/payment/webhook")]   // legacy URL still registered in the PayOS dashboard
    public async Task PayOS_Webhook_Should_Be_Reachable_Anonymously_On_Both_Routes(string url)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(url, new { });

        // Reaches the action (signature check fails -> 400); must not be 401 or 404.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Swagger_Document_Should_Generate_With_Versioned_Routes()
    {
        var provider = _factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();

        var document = provider.GetSwagger("v1");

        Assert.Contains("/api/v1/me/lessons/{lessonId}/progress", document.Paths.Keys);
        Assert.DoesNotContain(document.Paths.Keys, path => path.Contains("{version}"));
    }

    [Theory]
    [InlineData(ApiFactory.AllowedOrigin, true)]
    [InlineData("https://evil.example", false)]
    public async Task Cors_Should_Only_Allow_Configured_Origins(string origin, bool allowed)
    {
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/courses");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _factory.CreateClient().SendAsync(preflight);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_Should_Be_Public()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

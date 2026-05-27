using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<ApiResponse<CourseDetailResponse>> GetByIdAsync(int id)
    {
        var course = await _courseRepository.GetWithLessonsAsync(id);
        if (course == null)
            return ApiResponse<CourseDetailResponse>.FailResult("Course not found.");

        return ApiResponse<CourseDetailResponse>.SuccessResult(MapToDetailResponse(course));
    }

    public async Task<ApiResponse<CourseDetailResponse>> GetBySlugAsync(string slug)
    {
        var course = await _courseRepository.GetBySlugAsync(slug);
        if (course == null)
            return ApiResponse<CourseDetailResponse>.FailResult("Course not found.");

        return ApiResponse<CourseDetailResponse>.SuccessResult(MapToDetailResponse(course));
    }

    public async Task<PagedResponse<CourseResponse>> GetAllAsync(CourseQueryParams query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await _courseRepository.SearchAsync(
            query.Search, query.Category, query.Level, query.IsPublished,
            query.Page, query.PageSize, query.SortBy, query.SortDesc);

        return PagedResponse<CourseResponse>.SuccessResult(
            items.Select(MapToResponse), total, query.Page, query.PageSize);
    }

    public async Task<ApiResponse<CourseResponse>> CreateAsync(CreateCourseRequest request)
    {
        if (!Enum.TryParse<CourseLevel>(request.Level, true, out var level))
            return ApiResponse<CourseResponse>.FailResult("Invalid level.", $"Level '{request.Level}' is not valid.");

        var slug = GenerateSlug(request.Title);

        if (await _courseRepository.SlugExistsAsync(slug))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var course = new Course
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            Category = request.Category,
            Level = level,
            IsPublished = request.IsPublished
        };

        var created = await _courseRepository.AddAsync(course);
        return ApiResponse<CourseResponse>.SuccessResult(MapToResponse(created), "Course created successfully.");
    }

    public async Task<ApiResponse<CourseResponse>> UpdateAsync(int id, UpdateCourseRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null)
            return ApiResponse<CourseResponse>.FailResult("Course not found.");

        if (!Enum.TryParse<CourseLevel>(request.Level, true, out var level))
            return ApiResponse<CourseResponse>.FailResult("Invalid level.", $"Level '{request.Level}' is not valid.");

        // Regenerate slug only if title changed
        if (!course.Title.Equals(request.Title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var newSlug = GenerateSlug(request.Title);
            if (await _courseRepository.SlugExistsAsync(newSlug, id))
                newSlug = $"{newSlug}-{Guid.NewGuid().ToString()[..6]}";
            course.Slug = newSlug;
        }

        course.Title = request.Title.Trim();
        course.Description = request.Description;
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.Category = request.Category;
        course.Level = level;
        course.IsPublished = request.IsPublished;

        await _courseRepository.UpdateAsync(course);
        return ApiResponse<CourseResponse>.SuccessResult(MapToResponse(course), "Course updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (!await _courseRepository.ExistsAsync(id))
            return ApiResponse<bool>.FailResult("Course not found.");

        await _courseRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResult(true, "Course deleted successfully.");
    }
    public async Task<PagedResponse<CourseResponse>> GetByCategoryAsync(
    string category, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _courseRepository.SearchAsync(
            searchTerm: null,
            category: category,   // <-- truyền category vào đây
            level: null,
            isPublished: true,    // thường chỉ trả published
            page, pageSize,
            sortBy: "createdAt",
            sortDesc: true);

        return PagedResponse<CourseResponse>.SuccessResult(
            items.Select(MapToResponse), total, page, pageSize);
    }
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateSlug(string title)
        => System.Text.RegularExpressions.Regex
            .Replace(title.Trim().ToLower(), @"[^a-z0-9\s-]", "")
            .Replace(" ", "-")
            .Trim('-');

    private static CourseResponse MapToResponse(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Description = c.Description,
        ThumbnailUrl = c.ThumbnailUrl,
        Category = c.Category,
        Level = c.Level.ToString(),
        IsPublished = c.IsPublished,
        TotalDurationMinutes = c.TotalDurationMinutes,
        TotalLessons = c.TotalLessons,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static CourseDetailResponse MapToDetailResponse(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Description = c.Description,
        ThumbnailUrl = c.ThumbnailUrl,
        Category = c.Category,
        Level = c.Level.ToString(),
        IsPublished = c.IsPublished,
        TotalDurationMinutes = c.TotalDurationMinutes,
        TotalLessons = c.TotalLessons,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        EnrollmentCount = c.Enrollments?.Count ?? 0,
        Lessons = c.Lessons.Select(l => new LessonSummaryResponse
        {
            Id = l.Id,
            Title = l.Title,
            DurationMinutes = l.DurationMinutes,
            OrderIndex = l.OrderIndex,
            IsFree = l.IsFree
        }).ToList()
    };
}

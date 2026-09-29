using System.Text.RegularExpressions;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<PagedResult<CourseResponse>> GetAllAsync(CourseQueryParams query, bool includeUnpublished)
    {
        var isPublished = includeUnpublished ? query.IsPublished : true;
        var (sortBy, sortDesc) = query.ParseSort();

        var (items, total) = await _courseRepository.SearchAsync(
            query.Search, query.Category, query.Level?.ToString(), isPublished,
            query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<CourseResponse>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<CourseDetailResponse> GetByIdAsync(int id, bool includeUnpublished)
    {
        var course = await _courseRepository.GetWithLessonsAsync(id);
        return MapVisibleDetail(course, includeUnpublished, id);
    }

    public async Task<CourseDetailResponse> GetBySlugAsync(string slug, bool includeUnpublished)
    {
        var course = await _courseRepository.GetBySlugAsync(slug);
        return MapVisibleDetail(course, includeUnpublished, slug);
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request)
    {
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
            Level = request.Level,
            IsPublished = request.IsPublished
        };

        var created = await _courseRepository.AddAsync(course);
        return MapToResponse(created);
    }

    public async Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

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
        course.Level = request.Level;
        course.IsPublished = request.IsPublished;

        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _courseRepository.ExistsAsync(id))
            throw new NotFoundException("Course", id);

        await _courseRepository.DeleteAsync(id);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Draft courses are reported as missing to non-staff callers, so their existence is not leaked.
    private static CourseDetailResponse MapVisibleDetail(Course? course, bool includeUnpublished, object key)
    {
        if (course == null || (!course.IsPublished && !includeUnpublished))
            throw new NotFoundException("Course", key);

        return MapToDetailResponse(course);
    }

    private static string GenerateSlug(string title)
        => Regex.Replace(title.Trim().ToLowerInvariant(), @"[^a-z0-9\s-]", "")
            .Replace(' ', '-')
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

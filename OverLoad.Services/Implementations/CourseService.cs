using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
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

    // ── Public reads ─────────────────────────────────────────────────────────

    public async Task<PagedResult<CourseDto>> GetAllAsync(CourseQueryParams query, bool includeUnpublished)
    {
        var isPublished = includeUnpublished ? query.IsPublished : true;
        var (sortBy, sortDesc) = query.ParseSort();

        var (items, total) = await _courseRepository.SearchAsync(
            query.Search, query.Category, query.Level?.ToString(), isPublished,
            query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<CourseDto>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<CourseDetailDto> GetByIdAsync(int id, bool includeUnpublished)
    {
        var course = await _courseRepository.GetWithLessonsAsync(id);
        return MapVisibleDetail(course, includeUnpublished, id);
    }

    public async Task<CourseDetailDto> GetBySlugAsync(string slug, bool includeUnpublished)
    {
        var course = await _courseRepository.GetBySlugAsync(slug);
        return MapVisibleDetail(course, includeUnpublished, slug);
    }

    // ── Instructor reads ──────────────────────────────────────────────────────

    public async Task<List<CourseDto>> GetInstructorCoursesAsync(int instructorId)
    {
        var courses = await _courseRepository.GetByInstructorAsync(instructorId);
        return courses.Select(MapToResponse).ToList();
    }

    // ── Pending review (admin) ─────────────────────────────────────────────────

    public async Task<PagedResult<CourseDto>> GetPendingReviewAsync(CourseQueryParams query)
    {
        var (items, total) = await _courseRepository.GetPendingReviewAsync(query.PageNumber, query.PageSize);
        return PagedResult<CourseDto>.Create(items.Select(MapToResponse), total, query);
    }

    // ── Instructor writes ─────────────────────────────────────────────────────

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, int instructorId)
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
            InstructorId = instructorId,
            Status = CourseStatus.Draft
        };

        var created = await _courseRepository.AddAsync(course);
        return MapToResponse(created);
    }

    public async Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request, int callerId, bool isAdmin)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        CheckOwnership(course, callerId, isAdmin);

        if (!isAdmin && course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
            throw new ForbiddenException("You can only edit courses that are in Draft or Rejected state.");

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

        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    public async Task DeleteAsync(int id, int callerId, bool isAdmin)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        CheckOwnership(course, callerId, isAdmin);

        await _courseRepository.DeleteAsync(id);
    }

    // ── Approval workflow ─────────────────────────────────────────────────────

    public async Task<CourseDto> SubmitForReviewAsync(int id, int instructorId)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        if (course.InstructorId != instructorId)
            throw new ForbiddenException("You do not own this course.");

        if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
            throw new BusinessRuleException("Only Draft or Rejected courses can be submitted for review.");

        course.Status = CourseStatus.PendingReview;
        course.RejectionReason = null;
        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    public async Task<CourseDto> ApproveCourseAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        if (course.Status != CourseStatus.PendingReview)
            throw new BusinessRuleException("Only courses with PendingReview status can be approved.");

        course.Status = CourseStatus.Published;
        course.RejectionReason = null;
        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    public async Task<CourseDto> RejectCourseAsync(int id, string reason)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        if (course.Status != CourseStatus.PendingReview)
            throw new BusinessRuleException("Only courses with PendingReview status can be rejected.");

        course.Status = CourseStatus.Rejected;
        course.RejectionReason = reason.Trim();
        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    public async Task<CourseDto> SetPriceAsync(int id, decimal price)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        if (price < 0)
            throw new BusinessRuleException("Price cannot be negative.");

        course.Price = price;
        await _courseRepository.UpdateAsync(course);
        return MapToResponse(course);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void CheckOwnership(Course course, int callerId, bool isAdmin)
    {
        if (!isAdmin && course.InstructorId != callerId)
            throw new ForbiddenException("You do not own this course.");
    }

    // Draft courses are reported as missing to non-staff callers, so their existence is not leaked.
    private static CourseDetailDto MapVisibleDetail(Course? course, bool includeUnpublished, object key)
    {
        if (course == null || (!course.IsPublished && !includeUnpublished))
            throw new NotFoundException("Course", key);

        return MapToDetailResponse(course);
    }

    private static string GenerateSlug(string title)
    {
        // Replace đ/Đ first — they have no NFD decomposition
        var s = title.Trim().ToLowerInvariant().Replace('đ', 'd');

        // Decompose accented chars to base letter + combining diacritic, then strip diacritics
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        // Strip non-slug chars, collapse spaces/dashes
        var slug = Regex.Replace(sb.ToString(), @"[^a-z0-9\s-]", "").Replace(' ', '-');
        return Regex.Replace(slug, @"-{2,}", "-").Trim('-');
    }

    private static CourseDto MapToResponse(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Description = c.Description,
        ThumbnailUrl = c.ThumbnailUrl,
        Category = c.Category,
        Level = c.Level.ToString(),
        Status = c.Status.ToString(),
        RejectionReason = c.RejectionReason,
        InstructorId = c.InstructorId,
        TotalDurationMinutes = c.TotalDurationMinutes,
        TotalLessons = c.TotalLessons,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static CourseDetailDto MapToDetailResponse(Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Description = c.Description,
        ThumbnailUrl = c.ThumbnailUrl,
        Category = c.Category,
        Level = c.Level.ToString(),
        Status = c.Status.ToString(),
        RejectionReason = c.RejectionReason,
        InstructorId = c.InstructorId,
        TotalDurationMinutes = c.TotalDurationMinutes,
        TotalLessons = c.TotalLessons,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        EnrollmentCount = c.Enrollments?.Count ?? 0,
        Lessons = c.Lessons.Select(l => new LessonSummaryDto
        {
            Id = l.Id,
            Title = l.Title,
            DurationMinutes = l.DurationMinutes,
            OrderIndex = l.OrderIndex,
            IsFree = l.IsFree
        }).ToList()
    };
}

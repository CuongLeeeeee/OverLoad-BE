using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;

namespace OverLoad.Repositories.Interfaces;

public interface ICourseRepository : IBaseRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug);
    Task<Course?> GetWithLessonsAsync(int id);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    Task<List<Course>> GetByInstructorAsync(int instructorId);
    Task<(IEnumerable<Course> Items, int TotalCount)> SearchAsync(
        string? searchTerm, string? category, string? level, bool? isPublished,
        int page, int pageSize, string? sortBy, bool sortDesc);
    Task<(IEnumerable<Course> Items, int TotalCount)> GetPendingReviewAsync(
        int page, int pageSize);
}

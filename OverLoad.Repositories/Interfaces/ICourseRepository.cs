using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface ICourseRepository : IBaseRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug);
    Task<Course?> GetWithLessonsAsync(int id);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    Task<(IEnumerable<Course> Items, int TotalCount)> SearchAsync(
        string? searchTerm, string? category, string? level, bool? isPublished,
        int page, int pageSize, string? sortBy, bool sortDesc);
}

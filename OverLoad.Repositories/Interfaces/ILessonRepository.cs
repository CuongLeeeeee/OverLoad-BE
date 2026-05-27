using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface ILessonRepository : IBaseRepository<Lesson>
{
    Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId);
    Task<Lesson?> GetWithCourseAsync(int id);
    Task<int> GetNextOrderIndexAsync(int courseId);
    Task<(IEnumerable<Lesson> Items, int TotalCount)> SearchAsync(
        int? courseId, string? searchTerm, bool? isFree, int page, int pageSize, string? sortBy, bool sortDesc);
}

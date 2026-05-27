// OverLoad.Repositories/Interfaces/IUserLessonProgressRepository.cs
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface IUserLessonProgressRepository : IBaseRepository<UserLessonProgress>
{
    Task<UserLessonProgress?> GetByIdDetailAsync(int id);
    Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId);
    Task<IEnumerable<UserLessonProgress>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserLessonProgress>> GetByLessonIdAsync(int lessonId);
    Task<bool> ExistsByUserAndLessonAsync(int userId, int lessonId);
    Task<(IEnumerable<UserLessonProgress> Items, int TotalCount)> SearchAsync(
        int? userId, int? lessonId, bool? completed,
        int page, int pageSize, string? sortBy, bool sortDesc);
}
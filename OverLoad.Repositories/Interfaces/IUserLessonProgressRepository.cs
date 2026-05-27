using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface IUserLessonProgressRepository : IBaseRepository<UserLessonProgress>
{
    Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId);
    Task<IEnumerable<UserLessonProgress>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserLessonProgress>> GetByLessonIdAsync(int lessonId);
}

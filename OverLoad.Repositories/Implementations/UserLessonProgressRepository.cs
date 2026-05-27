using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class UserLessonProgressRepository : BaseRepository<UserLessonProgress>, IUserLessonProgressRepository
{
    public UserLessonProgressRepository(AppDbContext context) : base(context) { }

    public async Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId)
        => await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);

    public async Task<IEnumerable<UserLessonProgress>> GetByUserIdAsync(int userId)
        => await _dbSet.Include(p => p.Lesson)
                       .Where(p => p.UserId == userId)
                       .ToListAsync();

    public async Task<IEnumerable<UserLessonProgress>> GetByLessonIdAsync(int lessonId)
        => await _dbSet.Include(p => p.User)
                       .Where(p => p.LessonId == lessonId)
                       .ToListAsync();
}

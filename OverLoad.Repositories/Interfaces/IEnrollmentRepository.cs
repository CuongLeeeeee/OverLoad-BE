using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface IEnrollmentRepository : IBaseRepository<Enrollment>
{
    Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId);
    Task<IEnumerable<Enrollment>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Enrollment>> GetByCourseIdAsync(int courseId);
    Task<bool> IsEnrolledAsync(int userId, int courseId);
    Task<(IEnumerable<Enrollment> Items, int TotalCount)> SearchAsync(
        int? userId, int? courseId, int page, int pageSize, string? sortBy, bool sortDesc);
}

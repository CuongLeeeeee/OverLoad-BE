using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class CourseAccessService : ICourseAccessService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUserRepository _userRepository;

    public CourseAccessService(IEnrollmentRepository enrollmentRepository, IUserRepository userRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> HasPaidContentAccessAsync(int userId, int courseId)
        => await _enrollmentRepository.IsEnrolledAsync(userId, courseId)
           || await HasActiveProAsync(userId);

    public async Task<bool> HasActiveProAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null && user.HasActivePro(DateTime.UtcNow);
    }
}

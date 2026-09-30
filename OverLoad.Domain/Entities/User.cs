using OverLoad.Domain.Common;
using OverLoad.Domain.Enums;

namespace OverLoad.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsVerified { get; set; } = false;
    // JWT Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>Wallet balance in VND. Configured as a concurrency token so two debits cannot both succeed.</summary>
    public decimal WalletBalance { get; set; }

    /// <summary>PRO gives access to every paid course until this moment (UTC).</summary>
    public DateTime? ProExpiresAt { get; set; }

    public bool HasActivePro(DateTime utcNow) => ProExpiresAt > utcNow;

    /// <summary>Adds one package period, starting from the current expiry if PRO is still active.</summary>
    public void ExtendPro(ProPackage package, DateTime utcNow)
    {
        var start = HasActivePro(utcNow) ? ProExpiresAt!.Value : utcNow;
        ProExpiresAt = package == ProPackage.Year ? start.AddYears(1) : start.AddMonths(1);
    }

    // Navigation
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<UserLessonProgress> LessonProgresses { get; set; } = new List<UserLessonProgress>();
}

namespace OverLoad.API.Common;

/// <summary>Role names for [Authorize(Roles = ...)]; they match OverLoad.Domain.Enums.UserRole.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";

    /// <summary>Admin or Instructor.</summary>
    public const string Staff = Admin + "," + Instructor;
}

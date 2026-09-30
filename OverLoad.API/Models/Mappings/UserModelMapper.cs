using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.API.Models.Mappings;

public static class UserModelMapper
{
    public static RegisterRequest ToServiceDto(this RegisterRequestModel model) => new()
    {
        Email = model.Email,
        Password = model.Password,
        FullName = model.FullName,
        AvatarUrl = model.AvatarUrl,
        Bio = model.Bio
    };

    public static LoginRequest ToServiceDto(this LoginRequestModel model) => new()
    {
        Email = model.Email,
        Password = model.Password
    };

    public static RefreshTokenRequest ToServiceDto(this RefreshTokenRequestModel model) => new()
    {
        RefreshToken = model.RefreshToken
    };

    public static CreateUserRequest ToServiceDto(this CreateUserRequestModel model) => new()
    {
        Email = model.Email,
        Password = model.Password,
        FullName = model.FullName,
        AvatarUrl = model.AvatarUrl,
        Bio = model.Bio,
        Role = model.Role
    };

    public static UpdateUserRequest ToServiceDto(this UpdateUserRequestModel model) => new()
    {
        FullName = model.FullName,
        AvatarUrl = model.AvatarUrl,
        Bio = model.Bio,
        IsVerified = model.IsVerified,
        Role = model.Role
    };

    public static UpdateProfileRequest ToServiceDto(this UpdateProfileRequestModel model) => new()
    {
        FullName = model.FullName,
        AvatarUrl = model.AvatarUrl,
        Bio = model.Bio
    };

    public static UserQueryParams ToServiceDto(this UserQueryRequestModel model) =>
        new UserQueryParams { Search = model.Search, Role = model.Role }.WithPaging(model);

    public static UserResponseModel ToResponseModel(this UserDto dto) => Fill(new UserResponseModel(), dto);

    public static UserDetailResponseModel ToResponseModel(this UserDetailDto dto)
    {
        var model = Fill(new UserDetailResponseModel(), dto);
        model.Enrollments = dto.Enrollments.Select(e => new EnrollmentSummaryResponseModel
        {
            CourseId = e.CourseId,
            CourseTitle = e.CourseTitle,
            CourseSlug = e.CourseSlug,
            ProgressPercentage = e.ProgressPercentage,
            EnrolledAt = e.EnrolledAt,
            CompletedAt = e.CompletedAt,
            LastAccessedAt = e.LastAccessedAt
        }).ToList();
        return model;
    }

    public static AuthResponseModel ToResponseModel(this AuthResultDto dto) => new()
    {
        AccessToken = dto.AccessToken,
        RefreshToken = dto.RefreshToken,
        AccessTokenExpiresAt = dto.AccessTokenExpiresAt,
        User = dto.User.ToResponseModel()
    };

    private static TModel Fill<TModel>(TModel model, UserDto dto) where TModel : UserResponseModel
    {
        model.Id = dto.Id;
        model.Email = dto.Email;
        model.FullName = dto.FullName;
        model.AvatarUrl = dto.AvatarUrl;
        model.Bio = dto.Bio;
        model.Role = dto.Role;
        model.IsVerified = dto.IsVerified;
        model.IsLocked = dto.IsLocked;
        model.IsPro = dto.IsPro;
        model.ProExpiresAt = dto.ProExpiresAt;
        model.CreatedAt = dto.CreatedAt;
        model.UpdatedAt = dto.UpdatedAt;
        return model;
    }
}

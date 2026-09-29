using FluentValidation;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
        RuleFor(x => x.Bio).MaximumLength(1000);
    }
}

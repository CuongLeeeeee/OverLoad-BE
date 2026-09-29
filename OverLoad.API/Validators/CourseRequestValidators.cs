using FluentValidation;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Validators;

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Level).IsInEnum();
    }
}

public class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Level).IsInEnum();
    }
}

using FluentValidation;
using OverLoad.API.Models.Requests;

namespace OverLoad.API.Validators;

public class CreateCourseRequestModelValidator : AbstractValidator<CreateCourseRequestModel>
{
    public CreateCourseRequestModelValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Level).IsInEnum();
    }
}

public class UpdateCourseRequestModelValidator : AbstractValidator<UpdateCourseRequestModel>
{
    public UpdateCourseRequestModelValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Level).IsInEnum();
    }
}

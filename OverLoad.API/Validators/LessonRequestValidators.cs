using FluentValidation;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
    }
}

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(1);
    }
}

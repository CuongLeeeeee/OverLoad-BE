using FluentValidation;
using OverLoad.API.Models.Requests;

namespace OverLoad.API.Validators;

public class CreateLessonRequestModelValidator : AbstractValidator<CreateLessonRequestModel>
{
    public CreateLessonRequestModelValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
    }
}

public class UpdateLessonRequestModelValidator : AbstractValidator<UpdateLessonRequestModel>
{
    public UpdateLessonRequestModelValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(1);
    }
}

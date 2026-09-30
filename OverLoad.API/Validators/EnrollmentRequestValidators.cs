using FluentValidation;
using OverLoad.API.Models.Requests;

namespace OverLoad.API.Validators;

public class GrantEnrollmentRequestModelValidator : AbstractValidator<GrantEnrollmentRequestModel>
{
    public GrantEnrollmentRequestModelValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.CourseId).GreaterThan(0);
    }
}

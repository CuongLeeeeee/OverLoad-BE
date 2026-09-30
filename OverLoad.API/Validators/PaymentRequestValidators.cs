using FluentValidation;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        When(x => x.Type == CheckoutType.Course, () =>
        {
            RuleFor(x => x.CourseId).NotNull().GreaterThan(0);
        });

        When(x => x.Type == CheckoutType.Pro, () =>
        {
            RuleFor(x => x.Package).NotNull().IsInEnum();
        });

        // Deposit limits live in PaymentSettings and are enforced by PaymentService.
        When(x => x.Type == CheckoutType.Deposit, () =>
        {
            RuleFor(x => x.Amount).NotNull().GreaterThan(0);
        });

        RuleFor(x => x.ReturnUrl).MaximumLength(500);
        RuleFor(x => x.CancelUrl).MaximumLength(500);
    }
}

public class PurchaseProRequestValidator : AbstractValidator<PurchaseProRequest>
{
    public PurchaseProRequestValidator()
    {
        RuleFor(x => x.Package).IsInEnum();
    }
}

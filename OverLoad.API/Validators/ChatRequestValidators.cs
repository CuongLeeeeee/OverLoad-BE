using FluentValidation;
using OverLoad.API.Models.Requests;

namespace OverLoad.API.Validators;

public class ChatRequestModelValidator : AbstractValidator<ChatRequestModel>
{
    public ChatRequestModelValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleForEach(x => x.RecentHistory).ChildRules(item =>
        {
            item.RuleFor(h => h.Role).NotEmpty().Must(r => r is "user" or "model")
                .WithMessage("Role must be 'user' or 'model'.");
            item.RuleFor(h => h.Content).NotEmpty().MaximumLength(4000);
        });
    }
}

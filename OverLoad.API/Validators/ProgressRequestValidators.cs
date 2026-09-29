using FluentValidation;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Validators;

public class SaveProgressRequestValidator : AbstractValidator<SaveProgressRequest>
{
    public SaveProgressRequestValidator()
    {
        RuleFor(x => x.LastScrollPercentage).InclusiveBetween(0m, 100m);
        RuleFor(x => x.UnlockedCheckpointIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LastPositionSeconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WatchTimeSeconds).GreaterThanOrEqualTo(0);
    }
}

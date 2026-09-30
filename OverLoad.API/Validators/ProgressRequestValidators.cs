using FluentValidation;
using OverLoad.API.Models.Requests;

namespace OverLoad.API.Validators;

public class SaveProgressRequestModelValidator : AbstractValidator<SaveProgressRequestModel>
{
    public SaveProgressRequestModelValidator()
    {
        RuleFor(x => x.LastScrollPercentage).InclusiveBetween(0m, 100m);
        RuleFor(x => x.UnlockedCheckpointIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LastPositionSeconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WatchTimeSeconds).GreaterThanOrEqualTo(0);
    }
}

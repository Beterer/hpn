using FluentValidation;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;

internal sealed class UpdateProfileInterestsValidator : AbstractValidator<UpdateProfileInterestsRequest>
{
    public UpdateProfileInterestsValidator()
    {
        RuleFor(x => x.InterestIds)
            .NotNull();

        RuleFor(x => x.InterestIds)
            .Must(ids => ids is null || ids.Count <= 12)
            .WithMessage("Choose at most 12 interests.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Interest ids must be unique.");
    }
}

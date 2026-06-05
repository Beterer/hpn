using FluentValidation;
using Hpn.Modules.Profile.Internal.Domain;

namespace Hpn.Modules.Profile.Internal.Features.UpsertProfile;

internal sealed class UpsertProfileValidator : AbstractValidator<UpsertProfileRequest>
{
    public UpsertProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(80)
            .MinimumLength(2);

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(value => ProfileFormat.TryParseGender(value, out _))
            .WithMessage("Choose one of: woman, man, non_binary, self_describe.");

        RuleFor(x => x.SelfDescribeText)
            .MaximumLength(80)
            .When(x => !string.IsNullOrWhiteSpace(x.SelfDescribeText));

        RuleFor(x => x.SelfDescribeText)
            .NotEmpty()
            .When(x => string.Equals(x.Gender, "self_describe", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Self-describe text is required when gender is self_describe.");
    }
}

using FluentValidation;

namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

internal sealed class SubmitAppreciationValidator : AbstractValidator<SubmitAppreciationRequest>
{
    public SubmitAppreciationValidator()
    {
        RuleFor(r => r.ReceiverProfileId).NotEmpty();
        RuleFor(r => r.CategoryId).NotEmpty();
        RuleFor(r => r.PhotoId).Must(id => id is null || id.Value != Guid.Empty)
            .WithMessage("Photo id must be a valid id when supplied.");
    }
}

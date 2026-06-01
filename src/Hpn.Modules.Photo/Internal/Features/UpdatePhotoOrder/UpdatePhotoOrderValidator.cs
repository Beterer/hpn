using FluentValidation;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Photo.Internal.Features.UpdatePhotoOrder;

internal sealed class UpdatePhotoOrderValidator : AbstractValidator<UpdatePhotoOrderRequest>
{
    public UpdatePhotoOrderValidator(IOptions<PhotoUploadOptions> options)
    {
        var maxPhotos = options.Value.MaxPhotosPerProfile;

        RuleFor(x => x.PhotoIds)
            .NotEmpty()
            .WithMessage("At least one photo id is required.")
            .Must(ids => ids.Count <= maxPhotos)
            .WithMessage($"Profiles can have up to {maxPhotos} photos.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Photo ids must be unique.");
    }
}

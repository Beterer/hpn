namespace Hpn.Modules.Photo.Internal.Features.UpdatePhotoOrder;

internal sealed record UpdatePhotoOrderRequest(IReadOnlyCollection<Guid> PhotoIds);

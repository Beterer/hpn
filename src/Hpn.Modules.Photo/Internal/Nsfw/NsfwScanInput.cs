namespace Hpn.Modules.Photo.Internal.Nsfw;

internal sealed record NsfwScanInput(Guid ProfileId, string ContentHash, byte[] DisplayImage);

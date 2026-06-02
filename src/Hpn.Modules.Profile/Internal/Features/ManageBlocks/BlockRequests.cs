namespace Hpn.Modules.Profile.Internal.Features.ManageBlocks;

/// <summary>
/// Blocks are reached from a feed card or a report, where the caller holds the
/// target's <em>profile</em> id; the block itself is stored user-to-user (§7.3).
/// </summary>
internal sealed record BlockUserRequest(Guid TargetProfileId);

internal sealed record BlockedProfileResponse(Guid ProfileId, string DisplayName);

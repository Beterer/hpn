using Hpn.Modules.Feed.Contracts;
using Hpn.Modules.Feed.Contracts.Dtos;
using Hpn.Modules.Feed.Internal.Features.GetNext;

namespace Hpn.Modules.Feed.Internal;

/// <summary>
/// Cross-module implementation of <see cref="IFeedApi"/>. Delegates to the same
/// eligibility + ranking pipeline the endpoint uses; contract callers get the
/// ranked batch without the transport-only session-dedupe input.
/// </summary>
internal sealed class FeedApi(GetFeedNextHandler handler) : IFeedApi
{
    public Task<IReadOnlyList<FeedProfileDto>> GetNextAsync(
        Guid viewerId,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        handler.HandleAsync(viewerId, batchSize, [], cancellationToken);
}

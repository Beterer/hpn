using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Notification.Internal;

internal sealed class AppreciationReceivedNotificationHandler(
    IProfileApi profileApi,
    NotificationWriter writer)
    : IDomainEventHandler<AppreciationCreated>
{
    public async Task HandleAsync(AppreciationCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var userId = await profileApi.GetUserIdForProfileAsync(domainEvent.ReceiverProfileId, cancellationToken);
        if (userId is not { } recipient)
        {
            return;
        }

        await writer.CreateAppreciationReceivedAsync(
            recipient,
            domainEvent.AppreciationId,
            domainEvent.TraitLabel,
            domainEvent.CategorySlug,
            domainEvent.OccurredAt,
            cancellationToken);
    }
}

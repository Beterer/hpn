using System.Security.Claims;
using Hpn.SharedKernel.Auth;

namespace Hpn.Api.Infrastructure;

/// <summary>
/// Reads the authenticated principal off the current request for every module's
/// ownership checks (backbone §11). The session authentication handler in the
/// Identity module is what populates that principal; this only projects it.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public ActorKind ActorKind
    {
        get
        {
            var kind = Principal?.FindFirstValue(ActorClaims.KindClaimType);
            if (string.Equals(kind, ActorClaims.GuestKindValue, StringComparison.Ordinal))
            {
                return ActorKind.Guest;
            }

            if (string.Equals(kind, ActorClaims.MemberKindValue, StringComparison.Ordinal) || UserId is not null)
            {
                return ActorKind.Member;
            }

            return ActorKind.None;
        }
    }

    public Guid? ActorId
    {
        get
        {
            if (UserId is { } userId)
            {
                return userId;
            }

            return Guid.TryParse(Principal?.FindFirstValue(ActorClaims.IdClaimType), out var id) ? id : null;
        }
    }

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsAuthenticated => UserId is not null;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException("No authenticated user on the current request.");

    public Guid RequireActorId() =>
        ActorId ?? throw new InvalidOperationException("No authenticated actor on the current request.");
}

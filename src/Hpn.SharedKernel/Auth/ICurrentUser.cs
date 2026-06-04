namespace Hpn.SharedKernel.Auth;

/// <summary>
/// Ambient accessor for the authenticated principal of the current request
/// (backbone §11: ownership checks everywhere). Identity's session
/// authentication handler populates the principal; this exposes it to every
/// module's handlers without coupling them to ASP.NET Core or to Identity.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated actor kind: a member, a guest, or no actor.</summary>
    ActorKind ActorKind { get; }

    /// <summary>The member user id or guest id, or <c>null</c> for anonymous requests.</summary>
    Guid? ActorId { get; }

    /// <summary>The authenticated user's id, or <c>null</c> for anonymous requests.</summary>
    Guid? UserId { get; }

    /// <summary>True when a user is authenticated on the current request.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True when the authenticated user holds the given role.</summary>
    bool IsInRole(string role);

    /// <summary>The authenticated user's id, or throws if the request is anonymous.</summary>
    Guid RequireUserId();

    /// <summary>The authenticated actor's id, or throws if the request is anonymous.</summary>
    Guid RequireActorId();
}

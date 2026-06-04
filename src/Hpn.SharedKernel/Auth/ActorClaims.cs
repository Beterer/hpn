namespace Hpn.SharedKernel.Auth;

public static class ActorClaims
{
    public const string KindClaimType = "hpn:actor_kind";
    public const string IdClaimType = "hpn:actor_id";

    public const string MemberKindValue = "member";
    public const string GuestKindValue = "guest";
}

namespace Hpn.Modules.Profile.Internal.Domain;

internal sealed class ProfileInterest
{
    public Guid ProfileId { get; private set; }
    public Guid InterestId { get; private set; }
    public Interest Interest { get; private set; } = null!;

    private ProfileInterest()
    {
    }

    public static ProfileInterest Create(Guid profileId, Guid interestId) => new()
    {
        ProfileId = profileId,
        InterestId = interestId,
    };
}

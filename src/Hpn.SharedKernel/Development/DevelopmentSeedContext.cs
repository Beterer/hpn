using System.Collections.ObjectModel;

namespace Hpn.SharedKernel.Development;

public sealed class DevelopmentSeedContext(DevelopmentSeedOptions options)
{
    private readonly Dictionary<string, DevelopmentSeedUser> _users = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DevelopmentSeedProfile> _profiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<DevelopmentSeedPhoto>> _photosByProfileKey = new(StringComparer.Ordinal);

    public DevelopmentSeedOptions Options { get; } = options;

    public IReadOnlyDictionary<string, DevelopmentSeedUser> Users => new ReadOnlyDictionary<string, DevelopmentSeedUser>(_users);
    public IReadOnlyDictionary<string, DevelopmentSeedProfile> Profiles => new ReadOnlyDictionary<string, DevelopmentSeedProfile>(_profiles);

    public void SetUser(string key, Guid userId, string email) =>
        _users[key] = new DevelopmentSeedUser(key, userId, email);

    public DevelopmentSeedUser GetUser(string key) => _users[key];

    public void SetProfile(string key, Guid profileId, Guid userId) =>
        _profiles[key] = new DevelopmentSeedProfile(key, profileId, userId);

    public DevelopmentSeedProfile GetProfile(string key) => _profiles[key];

    public void SetPhotos(string profileKey, IReadOnlyCollection<DevelopmentSeedPhoto> photos) =>
        _photosByProfileKey[profileKey] = [.. photos.OrderBy(p => p.Position)];

    public IReadOnlyList<DevelopmentSeedPhoto> GetPhotos(string profileKey) =>
        _photosByProfileKey.TryGetValue(profileKey, out var photos) ? photos : [];

    public string CandidateKey(int index) => $"candidate:{index:D2}";

    public string ObserverKey(int index) => $"observer:{index:D2}";
}

public sealed record DevelopmentSeedUser(string Key, Guid UserId, string Email);

public sealed record DevelopmentSeedProfile(string Key, Guid ProfileId, Guid UserId);

public sealed record DevelopmentSeedPhoto(string ProfileKey, Guid ProfileId, Guid PhotoId, int Position);

namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>
/// The cached trust score for an account (backbone §7.8, §10.3), in [0,1]. Recomputed
/// from live signals when a moderation-relevant event fires (a report on or by the
/// user, or an action against them). <see cref="Signals"/> is the JSON breakdown that
/// produced <see cref="Score"/>, kept for auditability and tuning.
/// </summary>
internal sealed class AccountTrust
{
    public Guid UserId { get; private set; }
    public double Score { get; private set; }
    public string Signals { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; }

    private AccountTrust()
    {
    }

    public static AccountTrust Create(Guid userId, double score, string signals, DateTimeOffset now) => new()
    {
        UserId = userId,
        Score = score,
        Signals = signals,
        UpdatedAt = now,
    };

    public void Update(double score, string signals, DateTimeOffset now)
    {
        Score = score;
        Signals = signals;
        UpdatedAt = now;
    }
}

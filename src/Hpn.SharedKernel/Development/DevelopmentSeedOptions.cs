namespace Hpn.SharedKernel.Development;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";

    public bool Enabled { get; set; }
    public string TestEmail { get; set; } = "test@notice.local";
    public string ImageDirectory { get; set; } = "seed/images";
    public int CandidateCount { get; set; } = 30;
    public int IncomingAppreciationCount { get; set; } = 24;
    public int OutgoingAppreciationCount { get; set; } = 6;
}

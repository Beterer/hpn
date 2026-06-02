namespace Hpn.SharedKernel.Math;

/// <summary>
/// Shared rule for turning a count into a fractional share (0..1). Kept in one
/// place so the fingerprint distribution and the appreciation-style comparison
/// always round identically — otherwise the same counts could render different
/// percentages on the two screens.
/// </summary>
public static class ShareMath
{
    /// <summary>
    /// <paramref name="count"/> / <paramref name="total"/>, rounded to 4 decimals.
    /// Returns 0 when there is nothing to divide by.
    /// </summary>
    public static double Round(int count, int total) =>
        total <= 0 ? 0 : System.Math.Round(count / (double)total, 4, MidpointRounding.AwayFromZero);
}

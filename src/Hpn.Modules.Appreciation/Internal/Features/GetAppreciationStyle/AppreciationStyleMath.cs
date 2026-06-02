using Hpn.SharedKernel.Math;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;

internal static class AppreciationStyleMath
{
    public static double Share(int count, int total) => ShareMath.Round(count, total);

    public static double Difference(double userShare, double platformShare) =>
        System.Math.Round(userShare - platformShare, 4, MidpointRounding.AwayFromZero);
}

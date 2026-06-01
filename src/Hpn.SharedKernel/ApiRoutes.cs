namespace Hpn.SharedKernel;

/// <summary>
/// Single source of truth for the public API path prefix. The host mounts every
/// module group under this, and modules that emit absolute content URLs build
/// them from here so the two can never drift.
/// </summary>
public static class ApiRoutes
{
    public const string Prefix = "/api/v1";
}

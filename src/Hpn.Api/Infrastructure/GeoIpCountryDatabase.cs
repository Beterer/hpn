using System.Net;
using MaxMind.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hpn.Api.Infrastructure;

internal sealed class GeoIpOptions
{
    public const string SectionName = "GeoIp";

    /// <summary>
    /// Path to a GeoIP2-schema country database (<c>.mmdb</c>) — GeoLite2-Country or
    /// the free DB-IP Lite country file both work. Relative paths resolve against the
    /// content root. Empty/missing → IP-based country estimation is simply disabled.
    /// </summary>
    public string? DatabasePath { get; set; }
}

/// <summary>
/// Offline IP→country lookup over a memory-resident MaxMind-format database (ADR-028).
/// Loaded once at startup and shared (the reader is thread-safe). If no database is
/// configured/present, <see cref="LookupCountry"/> returns null and the same-country
/// filter is inert — no hard failure, no external call.
/// </summary>
internal sealed class GeoIpCountryDatabase : IDisposable
{
    private readonly Reader? _reader;
    private readonly ILogger<GeoIpCountryDatabase> _logger;

    public GeoIpCountryDatabase(
        IOptions<GeoIpOptions> options,
        IHostEnvironment environment,
        ILogger<GeoIpCountryDatabase> logger)
    {
        _logger = logger;

        var configured = options.Value.DatabasePath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogInformation("No GeoIp:DatabasePath configured; IP-based country estimation is disabled.");
            return;
        }

        var path = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);

        if (!File.Exists(path))
        {
            logger.LogWarning(
                "GeoIP database not found at {Path}; IP-based country estimation is disabled. Run `make geoip` to fetch it.",
                path);
            return;
        }

        // Load the whole file into memory (it is small) so we hold no file handle/lock.
        _reader = new Reader(path, FileAccessMode.Memory);
        _logger.LogInformation("GeoIP country database loaded from {Path}.", path);
    }

    public string? LookupCountry(IPAddress? ipAddress)
    {
        if (_reader is null || ipAddress is null)
        {
            return null;
        }

        try
        {
            var data = _reader.Find<Dictionary<string, object>>(ipAddress);

            // Two schemas in the wild: ip-location-db / DB-IP "country" files store a
            // flat top-level `country_code`; MaxMind GeoLite2/GeoIP2 files nest it under
            // `country`/`registered_country` → `iso_code`. Support both.
            return Flat(data, "country_code")
                ?? Nested(data, "country")
                ?? Nested(data, "registered_country");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {Ip}.", ipAddress);
            return null;
        }
    }

    private static string? Flat(IReadOnlyDictionary<string, object>? data, string key) =>
        data is not null &&
        data.TryGetValue(key, out var value) &&
        value is string code &&
        !string.IsNullOrWhiteSpace(code)
            ? code
            : null;

    private static string? Nested(IReadOnlyDictionary<string, object>? data, string node) =>
        data is not null &&
        data.TryGetValue(node, out var value) &&
        value is IReadOnlyDictionary<string, object> country
            ? Flat(country, "iso_code")
            : null;

    public void Dispose() => _reader?.Dispose();
}

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests;

/// <summary>
/// Proves the M0 harness: the host boots end-to-end over a real Postgres
/// (Testcontainers), the readiness probe actually reaches the database, and the
/// OpenAPI document is served. This is the seam every later milestone's
/// integration tests plug into.
/// </summary>
public sealed class SmokeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(TestContext.Current.CancellationToken);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("DevelopmentSeed:Enabled", "false");
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Readiness_probe_succeeds_against_real_postgres()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/health/ready", cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).Should().Be("Healthy");
    }

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}

using FluentAssertions;
using Hpn.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hpn.SharedKernel.Tests;

public sealed class InProcessDomainEventDispatcherTests
{
    private sealed record TestEvent(string Payload) : IDomainEvent;

    private sealed class RecordingHandler(List<string> log, string name) : IDomainEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            log.Add($"{name}:{domainEvent.Payload}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DispatchAsync_invokes_every_registered_handler_in_registration_order()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddDomainEventDispatcher();
        services.AddScoped<IDomainEventHandler<TestEvent>>(sp =>
            new RecordingHandler(sp.GetRequiredService<List<string>>(), "first"));
        services.AddScoped<IDomainEventHandler<TestEvent>>(sp =>
            new RecordingHandler(sp.GetRequiredService<List<string>>(), "second"));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new TestEvent("hi"), TestContext.Current.CancellationToken);

        log.Should().Equal("first:hi", "second:hi");
    }

    [Fact]
    public async Task DispatchAsync_with_no_registered_handlers_is_a_noop()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatcher();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var act = async () => await dispatcher.DispatchAsync(
            new TestEvent("x"), TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}

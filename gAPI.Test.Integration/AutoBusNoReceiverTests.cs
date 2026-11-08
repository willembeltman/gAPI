using Aspire.Hosting;
using Aspire.Hosting.Testing;
using gAPI.Core.Helpers;
using gAPI.Core.Server.Fabric;
using gAPI.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace gAPI.Test.Integration;

public sealed class AutoBusNoReceiverTests : IClassFixture<AutoBusFabricFixture>
{
    private readonly AutoBusFabricFixture fixture;

    public AutoBusNoReceiverTests(AutoBusFabricFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ToAll_WithThrowIfNoReceiverFalse_ReturnsEmptyStream()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var results = new List<string>();

        await foreach (var result in fixture.BusContext.MainBus
            .ToAll(TimeSpan.FromSeconds(10), throwIfNoReceiver: false)
            .InvokeList("no receiver", cts.Token))
        {
            results.Add(result);
        }

        Assert.Empty(results);
    }

    [Fact]
    public async Task ToAll_WithoutReceiverOption_ThrowsByDefault()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await Assert.ThrowsAsync<RemoteException>(async () =>
        {
            await foreach (var _ in fixture.BusContext.MainBus
                .ToAll(TimeSpan.FromSeconds(10))
                .InvokeList("no receiver", cts.Token))
            {
            }
        });
    }
}

public sealed class AutoBusFabricFixture : IAsyncLifetime
{
    private DistributedApplication? application;
    private IHost? clientHost;

    public IBusContext BusContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.gAPI_Test_Integration_AppHost>();
        application = await appHostBuilder.BuildAsync();
        await application.StartAsync();

        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["QueueName"] = "Standard",
            ["ConnectionStrings:ServiceBus"] = "Server=localhost;Port=9595"
        });
        hostBuilder.Services.AddAutoBus(hostBuilder.Configuration);
        clientHost = hostBuilder.Build();

        await clientHost.StartAsync();

        var fabricClient = clientHost.Services.GetRequiredService<BusFabricClient>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (fabricClient.FabricConnectionId.Value < 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        }

        BusContext = clientHost.Services.GetRequiredService<IBusContext>();
    }

    public async Task DisposeAsync()
    {
        if (clientHost != null)
        {
            await clientHost.StopAsync();
            if (clientHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                clientHost.Dispose();
            }
        }

        if (application != null)
        {
            await application.DisposeAsync();
        }
    }
}

using Aspire.Hosting.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wisp.Gateway.Features.Guilds;
using Wisp.Gateway.Features.TempVoice;
using Wisp.Persistence;

[assembly: AssemblyFixture(typeof(Wisp.Gateway.Tests.Infrastructure.DatabaseFixture))]

namespace Wisp.Gateway.Tests.Infrastructure;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = DistributedApplicationTestingBuilder.Create();

        builder.AddPostgres("postgres").AddDatabase("database", "wisp");

        _app = await builder.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("database");

        ConnectionString = await _app.GetConnectionStringAsync("database")
                           ?? throw new InvalidOperationException("Aspire didn't provide a connection string for 'database'.");

        using var host = CreateHost();
        await host.MigrateDatabaseAsync();
    }

    public IHost CreateHost(Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:database"] = ConnectionString;

        builder.AddPersistence();
        builder.Services.AddGuilds();
        builder.Services.AddTempVoice();

        configureServices?.Invoke(builder.Services);

        return builder.Build();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}

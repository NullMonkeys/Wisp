using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wisp.Gateway.Features.Guilds;
using Wisp.Gateway.Features.JoinRoles;
using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features.JoinRoles;

/// <summary>
/// One test's world: its own guild, the production services wired to <see cref="FakeJoinRolesDiscord"/>, and shortcuts
/// that mirror what the commands and gateway handlers do. Every call runs in its own DI scope, like every event does.
/// </summary>
public sealed class JoinRolesHarness : IAsyncDisposable
{
    private readonly IHost _host;

    private JoinRolesHarness(IHost host, FakeJoinRolesDiscord discord)
    {
        _host = host;
        Discord = discord;
    }

    public FakeJoinRolesDiscord Discord { get; }

    public ulong GuildId => Discord.GuildId;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<JoinRolesHarness> CreateAsync(DatabaseFixture database)
    {
        var discord = new FakeJoinRolesDiscord(TestIds.Next());
        var host = database.CreateHost(services =>
        {
            services.AddSingleton<IJoinRolesDiscord>(discord);
            services.AddSingleton<IJoinRolesGuildState>(discord);
        });

        var harness = new JoinRolesHarness(host, discord);
        await harness.InScope<IGuildsService>(s => s.EnsureGuildAsync(harness.GuildId, "Test guild", Ct));
        return harness;
    }

    #region Settings (what the commands and role select call)

    public Task<SetRolesResult> SetRolesAsync(params ulong[] roleIds)
        => InScope<IJoinRolesSettingsService, SetRolesResult>(s => s.SetRolesAsync(GuildId, roleIds, Ct));

    public Task<JoinRolesConfig> EnableAsync()
        => InScope<IJoinRolesSettingsService, JoinRolesConfig>(s => s.EnableAsync(GuildId, Ct));

    public Task<JoinRolesConfig> DisableAsync()
        => InScope<IJoinRolesSettingsService, JoinRolesConfig>(s => s.DisableAsync(GuildId, Ct));

    public Task<JoinRolesConfig> GetConfigAsync()
        => InScope<IJoinRolesSettingsService, JoinRolesConfig>(s => s.GetAsync(GuildId, Ct));

    /// <summary>
    /// Picks <paramref name="roleIds"/> and enables join roles.
    /// </summary>
    public async Task SetUpAsync(params ulong[] roleIds)
    {
        await SetRolesAsync(roleIds);
        await EnableAsync();
    }

    #endregion

    #region Gateway events

    public Task<int> JoinAsync(ulong userId, bool isBot = false, bool isPending = false, ulong[]? currentRoles = null)
        => InScope<IJoinRolesMembersService, int>(s =>
            s.MemberJoinedAsync(GuildId, userId, isBot, isPending, currentRoles ?? [], Ct));

    public Task<int> UpdateAsync(ulong userId, bool wasPending, bool isPending, bool isBot = false,
        ulong[]? currentRoles = null)
        => InScope<IJoinRolesMembersService, int>(s =>
            s.MemberUpdatedAsync(GuildId, userId, isBot, wasPending, isPending, currentRoles ?? [], Ct));

    /// <summary>
    /// The role is deleted, then the role-delete handler runs.
    /// </summary>
    public Task DeleteRoleAsync(ulong roleId)
    {
        Discord.DeleteRole(roleId);
        return InScope<IJoinRolesSettingsService>(s => s.ForgetRoleAsync(GuildId, roleId, Ct));
    }

    public Task DeleteGuildAsync()
        => InScope<IGuildsService>(s => s.DeleteGuildAsync(GuildId, Ct));

    #endregion

    private async Task InScope<TService>(Func<TService, Task> action) where TService : notnull
    {
        await using var scope = _host.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private async Task<T> InScope<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public ValueTask DisposeAsync()
    {
        _host.Dispose();
        return ValueTask.CompletedTask;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wisp.Gateway.Features.Guilds;
using Wisp.Gateway.Features.TempVoice;
using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features.TempVoice;

/// <summary>
/// One test's world: its own guild, the production services wired to <see cref="FakeDiscord"/>, and shortcuts that
/// mirror what the gateway handlers do. Every call runs in its own DI scope, like every gateway event does.
/// </summary>
public sealed class TempVoiceHarness : IAsyncDisposable
{
    private readonly IHost _host;

    private TempVoiceHarness(IHost host, FakeDiscord discord)
    {
        _host = host;
        Discord = discord;
    }

    public FakeDiscord Discord { get; }

    public ulong GuildId { get; } = TestIds.Next();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <param name="seedGuild">
    /// Whether to save the guild row first, as GUILD_CREATE would. Without it, anything referencing the guild fails
    /// its foreign key — handy for making a save fail on purpose.
    /// </param>
    public static async Task<TempVoiceHarness> CreateAsync(DatabaseFixture database, bool seedGuild = true)
    {
        var discord = new FakeDiscord();
        var host = database.CreateHost(services =>
        {
            services.AddSingleton<ITempVoiceDiscord>(discord);
            services.AddSingleton<ITempVoiceGuildState>(discord);
        });

        var harness = new TempVoiceHarness(host, discord);

        if (seedGuild)
            await harness.InScope<IGuildsService>(s => s.EnsureGuildAsync(harness.GuildId, "Test guild", Ct));

        return harness;
    }

    #region Settings

    public Task<TempVoiceConfig> EnableAsync(bool force = false)
        => InScope<ITempVoiceSettingsService, TempVoiceConfig>(s => s.EnableAsync(GuildId, force, Ct));

    public Task<TempVoiceConfig> DisableAsync()
        => InScope<ITempVoiceSettingsService, TempVoiceConfig>(s => s.DisableAsync(GuildId, Ct));

    public Task<TempVoiceConfig> GetConfigAsync()
        => InScope<ITempVoiceSettingsService, TempVoiceConfig>(s => s.GetAsync(GuildId, Ct));

    /// <summary>
    /// Enables temp voice and returns the master channel's ID.
    /// </summary>
    public async Task<ulong> EnableAndGetMasterAsync()
        => (await EnableAsync()).MasterChannelId!.Value;

    #endregion

    #region Channels (what the gateway handlers call)

    /// <summary>
    /// The user connects to <paramref name="channelId"/>, then the voice-state handler runs.
    /// </summary>
    /// <returns>The channel the user ends up in.</returns>
    public async Task<ulong?> JoinAsync(ulong userId, ulong channelId, string? displayName = "Alice")
    {
        Discord.Connect(GuildId, userId, channelId);
        await InScope<ITempVoiceChannelsService>(s => s.UserJoinedAsync(GuildId, userId, channelId, displayName, Ct));
        return Discord.ChannelOf(GuildId, userId);
    }

    /// <summary>
    /// The user disconnects from whatever channel they're in, then the voice-state handler runs.
    /// </summary>
    public async Task LeaveAsync(ulong userId)
    {
        var channelId = Discord.ChannelOf(GuildId, userId)
                        ?? throw new InvalidOperationException($"User {userId} isn't in voice.");

        Discord.Disconnect(GuildId, userId);
        await InScope<ITempVoiceChannelsService>(s => s.UserLeftAsync(GuildId, channelId, userId, Ct));
    }

    public Task SavePreferencesAsync(ulong channelId, TempChannelSettings before, TempChannelSettings after)
        => InScope<ITempVoiceChannelsService>(s => s.SavePreferencesAsync(channelId, before, after, Ct));

    public Task<bool> ForgetChannelAsync(ulong channelId)
        => InScope<ITempVoiceChannelsService, bool>(s => s.ForgetChannelAsync(channelId, Ct));

    /// <summary>
    /// Runs reconciliation with the guild as the fake Discord currently sees it, as GUILD_CREATE would.
    /// </summary>
    public Task ReconcileAsync()
        => InScope<ITempVoiceChannelsService>(s =>
            s.ReconcileAsync(GuildId, Discord.ChannelIds(GuildId), Discord.OccupiedChannelIds(GuildId), Ct));

    public Task DeleteGuildAsync()
        => InScope<IGuildsService>(s => s.DeleteGuildAsync(GuildId, Ct));

    #endregion

    #region Database

    public Task<List<TempChannel>> TempChannelsAsync()
        => QueryAsync(db => db.TempChannels.Where(t => t.GuildId == GuildId).ToListAsync(Ct));

    public Task<TempVoicePreferences?> PreferencesAsync(ulong userId)
        => QueryAsync(db => db.TempVoicePreferences.FirstOrDefaultAsync(p => p.GuildId == GuildId && p.UserId == userId, Ct));

    public async Task SeedPreferencesAsync(TempVoicePreferences preferences)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WispDbContext>();
        db.TempVoicePreferences.Add(preferences);
        await db.SaveChangesAsync(Ct);
    }

    public async Task<T> QueryAsync<T>(Func<WispDbContext, Task<T>> query)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WispDbContext>();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return await query(db);
    }

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

using Microsoft.EntityFrameworkCore;

using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Common;
using Wisp.Persistence;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice;

internal sealed partial class TempVoiceSettingsService(
    ILogger<TempVoiceSettingsService> logger,
    ITempVoiceDiscord discord,
    WispDbContext dbContext,
    ITempVoiceChannelsService channelsService) : ITempVoiceSettingsService
{
    private static readonly KeyedLock<ulong> GuildLocks = new();

    public async Task<TempVoiceConfig> GetAsync(ulong guildId, CancellationToken ct = default)
    {
        return await dbContext
                   .TempVoiceConfigs
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.GuildId == guildId, ct)
               ?? new TempVoiceConfig { GuildId = guildId };
    }

    public Task<TempVoiceConfig> EnableAsync(ulong guildId, CancellationToken ct = default)
        => WithGuildLockAsync(guildId, () => EnableCoreAsync(guildId, ct), ct);

    public Task<TempVoiceConfig> DisableAsync(ulong guildId, CancellationToken ct = default)
        => WithGuildLockAsync(guildId, () => DisableCoreAsync(guildId, ct), ct);

    private static async Task<T> WithGuildLockAsync<T>(ulong guildId, Func<Task<T>> action, CancellationToken ct)
    {
        using var _ = await GuildLocks.AcquireAsync(guildId, ct);
        return await action();
    }

    private async Task<TempVoiceConfig> EnableCoreAsync(ulong guildId, CancellationToken ct)
    {
        var config = await dbContext.TempVoiceConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);

        switch (config)
        {
            case { Enabled: true } when await ChannelsExistAsync(config, ct):
                return config;
            case { Enabled: true }:
                await channelsService.DeleteAllAsync(guildId, ct);
                break;
        }

        var oldCategoryId = config?.CategoryId;
        var oldMasterChannelId = config?.MasterChannelId;

        // The old category and master channel are deleted only after the new ones are saved,
        // so a failure from here on leaves the previous setup working.
        var categoryId = await CreateCategory(guildId, ct);

        ulong masterChannelId;
        try
        {
            masterChannelId = await CreateMasterChannel(guildId, categoryId, ct);
        }
        catch
        {
            await TryDeleteChannelAsync(categoryId);
            throw;
        }

        if (config is null)
        {
            config = new TempVoiceConfig { GuildId = guildId };
            dbContext.TempVoiceConfigs.Add(config);
        }

        config.Enabled = true;
        config.CategoryId = categoryId;
        config.MasterChannelId = masterChannelId;

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteChannelAsync(masterChannelId);
            await TryDeleteChannelAsync(categoryId);
            throw;
        }

        await TryDeleteChannelAsync(oldMasterChannelId);
        await TryDeleteChannelAsync(oldCategoryId);

        LogEnabled(guildId, categoryId, masterChannelId);
        return config;
    }

    private async Task<TempVoiceConfig> DisableCoreAsync(ulong guildId, CancellationToken ct)
    {
        var config = await dbContext.TempVoiceConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);

        if (config is not { Enabled: true })
            return config ?? new TempVoiceConfig { GuildId = guildId };

        var categoryId = config.CategoryId;
        var masterChannelId = config.MasterChannelId;

        config.Enabled = false;
        config.CategoryId = null;
        config.MasterChannelId = null;
        await dbContext.SaveChangesAsync(ct);

        await channelsService.DeleteAllAsync(guildId, CancellationToken.None);
        await TryDeleteChannelAsync(masterChannelId);
        await TryDeleteChannelAsync(categoryId);

        LogDisabled(guildId);
        return config;
    }

    private Task<ulong> CreateCategory(ulong guildId, CancellationToken ct = default)
    {
        return discord.CreateChannelAsync(
            guildId,
            new GuildChannelProperties("Temporary Voice", ChannelType.CategoryChannel),
            ct);
    }

    private Task<ulong> CreateMasterChannel(ulong guildId, ulong categoryId, CancellationToken ct = default)
    {
        return discord.CreateChannelAsync(
            guildId,
            new GuildChannelProperties("[+] Create channel", ChannelType.VoiceGuildChannel)
            {
                ParentId = categoryId
            },
            ct);
    }

    private async Task<bool> ChannelsExistAsync(TempVoiceConfig config, CancellationToken ct)
    {
        return config is { CategoryId: { } categoryId, MasterChannelId: { } masterChannelId }
               && await discord.ChannelExistsAsync(categoryId, ct)
               && await discord.ChannelExistsAsync(masterChannelId, ct);
    }

    private async Task TryDeleteChannelAsync(ulong? channelId)
    {
        if (channelId is not { } id)
            return;

        try
        {
            await discord.DeleteChannelIfExistsAsync(id);
        }
        catch (Exception e)
        {
            LogChannelCleanupFailed(e, id);
        }
    }

    [LoggerMessage(LogLevel.Information, "Temp voice enabled in guild {GuildId} (category {CategoryId}, master channel {MasterChannelId})")]
    partial void LogEnabled(ulong guildId, ulong categoryId, ulong masterChannelId);

    [LoggerMessage(LogLevel.Information, "Temp voice disabled in guild {GuildId}")]
    partial void LogDisabled(ulong guildId);

    [LoggerMessage(LogLevel.Warning, "Failed to delete channel {ChannelId} during temp voice cleanup")]
    partial void LogChannelCleanupFailed(Exception exception, ulong channelId);
}

using System.Net;

using Microsoft.EntityFrameworkCore;

using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Common;
using Wisp.Persistence;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice;

internal sealed partial class TempVoiceChannelsService(
    ILogger<TempVoiceChannelsService> logger,
    ITempVoiceDiscord discord,
    ITempVoiceGuildState guildState,
    WispDbContext dbContext) : ITempVoiceChannelsService
{
    private static readonly KeyedLock<(ulong GuildId, ulong UserId)> OwnerLocks = new();

    private const Permissions OwnerPermissions =
        Permissions.ViewChannel
        | Permissions.Connect
        | Permissions.Speak
        | Permissions.ManageChannels // rename, user limit, bitrate, region (and delete)
        | Permissions.ManageRoles    // on a channel this is "Manage Permissions"
        | Permissions.MoveUsers      // disconnect people from their channel
        | Permissions.MuteUsers
        | Permissions.DeafenUsers;

    private const Permissions BotPermissions =
        Permissions.ViewChannel
        | Permissions.Connect
        | Permissions.ManageChannels
        | Permissions.MoveUsers;

    public async Task UserJoinedAsync(ulong guildId, ulong userId, ulong channelId, string? displayName,
        CancellationToken ct = default)
    {
        var config = await dbContext.TempVoiceConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.MasterChannelId == channelId && c.Enabled, ct);

        if (config is not { CategoryId: { } categoryId })
            return;

        using var _ = await OwnerLocks.AcquireAsync((guildId, userId), ct);

        var existing = await dbContext.TempChannels
            .FirstOrDefaultAsync(t => t.GuildId == guildId && t.OwnerId == userId, ct);

        if (existing is not null)
        {
            if (guildState.ChannelExists(guildId, existing.ChannelId))
            {
                await TryMoveUserAsync(guildId, userId, existing.ChannelId, ct);
                return;
            }

            dbContext.TempChannels.Remove(existing);
            await dbContext.SaveChangesAsync(ct);
        }

        var preferences = await dbContext.TempVoicePreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.UserId == userId, ct);

        var categoryOverwrites = guildState.GetPermissionOverwrites(guildId, categoryId);
        var tempChannelId = await CreateChannelAsync(guildId, categoryId, categoryOverwrites, userId, displayName,
            preferences, ct);

        dbContext.TempChannels.Add(new TempChannel { ChannelId = tempChannelId, GuildId = guildId, OwnerId = userId });

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteChannelAsync(tempChannelId);
            throw;
        }

        if (!await TryMoveUserAsync(guildId, userId, tempChannelId, ct))
        {
            // They left before we could move them — don't leave an empty channel behind.
            await TryDeleteChannelAsync(tempChannelId);
            await dbContext.TempChannels
                .Where(t => t.ChannelId == tempChannelId)
                .ExecuteDeleteAsync(CancellationToken.None);
            return;
        }

        LogCreated(tempChannelId, guildId, userId);
    }

    public Task<int> CountActiveAsync(ulong guildId, CancellationToken ct = default)
        => dbContext.TempChannels.CountAsync(t => t.GuildId == guildId, ct);

    public async Task UserLeftAsync(ulong guildId, ulong channelId, ulong userId, CancellationToken ct = default)
    {
        var tempChannel = await dbContext.TempChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ChannelId == channelId, ct);

        if (tempChannel is null)
            return;

        using var _ = await OwnerLocks.AcquireAsync((guildId, tempChannel.OwnerId), ct);

        if (guildState.HasVoiceMembers(guildId, channelId, exceptUserId: userId))
            return;

        await TryDeleteChannelAsync(channelId);
        await dbContext.TempChannels
            .Where(t => t.ChannelId == channelId)
            .ExecuteDeleteAsync(CancellationToken.None);

        LogDeleted(channelId, guildId);
    }

    public async Task SavePreferencesAsync(ulong channelId, TempChannelSettings before, TempChannelSettings after,
        CancellationToken ct = default)
    {
        var tempChannel = await dbContext.TempChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ChannelId == channelId, ct);

        if (tempChannel is null)
            return;

        var guildId = tempChannel.GuildId;
        var ownerId = tempChannel.OwnerId;

        using var _ = await OwnerLocks.AcquireAsync((guildId, ownerId), ct);

        var preferences = await dbContext.TempVoicePreferences
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.UserId == ownerId, ct);

        var isNew = preferences is null;
        preferences ??= new TempVoicePreferences { GuildId = guildId, UserId = ownerId };

        var changed = false;

        if (before.Name != after.Name)
        {
            preferences.Name = after.Name;
            changed = true;
        }

        if (before.UserLimit != after.UserLimit)
        {
            preferences.UserLimit = after.UserLimit;
            changed = true;
        }

        if (before.Bitrate != after.Bitrate)
        {
            preferences.Bitrate = after.Bitrate;
            changed = true;
        }

        var afterOverwrites = UserOverwrites(after.Overwrites, ownerId);
        if (!UserOverwrites(before.Overwrites, ownerId).SetEquals(afterOverwrites))
        {
            var categoryOverwrites = after.CategoryId is { } categoryId
                ? guildState.GetPermissionOverwrites(guildId, categoryId).ToDictionary(o => o.Id)
                : [];

            preferences.Overwrites.Clear();

            foreach (var overwrite in afterOverwrites)
            {
                if (categoryOverwrites.TryGetValue(overwrite.Id, out var inherited)
                    && inherited.Allowed == overwrite.Allowed
                    && inherited.Denied == overwrite.Denied)
                    continue;

                preferences.Overwrites.Add(new TempVoiceOverwrite
                {
                    Id = overwrite.Id,
                    Kind = (PermissionOverwriteKind)overwrite.Type,
                    Allowed = (ulong)overwrite.Allowed,
                    Denied = (ulong)overwrite.Denied,
                });
            }

            changed = true;
        }

        if (!changed)
            return;

        if (isNew)
            dbContext.TempVoicePreferences.Add(preferences);

        await dbContext.SaveChangesAsync(ct);
        LogPreferencesSaved(ownerId, guildId);
    }

    public async Task<bool> ForgetChannelAsync(ulong channelId, CancellationToken ct = default)
    {
        var affected = await dbContext.TempChannels
            .Where(t => t.ChannelId == channelId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }

    public async Task DeleteAllAsync(ulong guildId, CancellationToken ct = default)
    {
        var channelIds = await dbContext.TempChannels
            .Where(t => t.GuildId == guildId)
            .Select(t => t.ChannelId)
            .ToListAsync(ct);

        foreach (var channelId in channelIds)
            await TryDeleteChannelAsync(channelId);

        await dbContext.TempChannels
            .Where(t => t.GuildId == guildId)
            .ExecuteDeleteAsync(CancellationToken.None);
    }

    public async Task ReconcileAsync(ulong guildId, IReadOnlySet<ulong> existingChannelIds,
        IReadOnlySet<ulong> occupiedChannelIds, CancellationToken ct = default)
    {
        var tempChannels = await dbContext.TempChannels
            .Where(t => t.GuildId == guildId)
            .ToListAsync(ct);

        var stale = new List<TempChannel>();

        foreach (var tempChannel in tempChannels)
        {
            if (!existingChannelIds.Contains(tempChannel.ChannelId))
            {
                stale.Add(tempChannel);
                continue;
            }

            if (occupiedChannelIds.Contains(tempChannel.ChannelId))
                continue;

            await TryDeleteChannelAsync(tempChannel.ChannelId);
            stale.Add(tempChannel);
        }

        if (stale.Count == 0)
            return;

        dbContext.TempChannels.RemoveRange(stale);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        LogReconciled(guildId, stale.Count);
    }

    private async Task<ulong> CreateChannelAsync(ulong guildId, ulong categoryId,
        IReadOnlyCollection<PermissionOverwriteSnapshot> categoryOverwrites, ulong ownerId, string? displayName,
        TempVoicePreferences? preferences, CancellationToken ct)
    {
        if (preferences is not null)
        {
            try
            {
                return await discord.CreateChannelAsync(guildId,
                    BuildChannel(categoryId, categoryOverwrites, ownerId, displayName, preferences), ct);
            }
            catch (RestException e) when (e.StatusCode == HttpStatusCode.BadRequest)
            {
                // Saved preferences may no longer be valid in this server; fall back to defaults
                // rather than leave the user without a channel.
                LogPreferencesRejected(e, ownerId, guildId);
            }
        }

        return await discord.CreateChannelAsync(guildId,
            BuildChannel(categoryId, categoryOverwrites, ownerId, displayName, preferences: null), ct);
    }

    private GuildChannelProperties BuildChannel(ulong categoryId,
        IReadOnlyCollection<PermissionOverwriteSnapshot> categoryOverwrites, ulong ownerId, string? displayName,
        TempVoicePreferences? preferences)
    {
        var overwrites = new Dictionary<ulong, PermissionOverwriteProperties>();

        foreach (var o in categoryOverwrites)
            overwrites[o.Id] = new PermissionOverwriteProperties(o.Id, o.Type) { Allowed = o.Allowed, Denied = o.Denied };

        if (preferences is not null)
        {
            foreach (var o in preferences.Overwrites)
            {
                overwrites[o.Id] = new PermissionOverwriteProperties(o.Id, (PermissionOverwriteType)o.Kind)
                {
                    Allowed = (Permissions)o.Allowed,
                    Denied = (Permissions)o.Denied,
                };
            }
        }

        var botId = guildState.BotId;
        overwrites[botId] = new PermissionOverwriteProperties(botId, PermissionOverwriteType.User) { Allowed = BotPermissions };
        overwrites[ownerId] = new PermissionOverwriteProperties(ownerId, PermissionOverwriteType.User) { Allowed = OwnerPermissions };

        var name = preferences?.Name ?? (displayName is null ? "Voice channel" : $"{displayName}'s channel");

        return new GuildChannelProperties(name, ChannelType.VoiceGuildChannel)
        {
            ParentId = categoryId,
            PermissionOverwrites = overwrites.Values,
            UserLimit = preferences?.UserLimit,
            Bitrate = preferences?.Bitrate,
        };
    }

    private HashSet<PermissionOverwriteSnapshot> UserOverwrites(IEnumerable<PermissionOverwriteSnapshot> overwrites,
        ulong ownerId)
        => [.. overwrites.Where(o => o.Id != ownerId && o.Id != guildState.BotId)];

    private async Task<bool> TryMoveUserAsync(ulong guildId, ulong userId, ulong channelId, CancellationToken ct)
    {
        try
        {
            await discord.MoveUserAsync(guildId, userId, channelId, ct);
            return true;
        }
        catch (RestException e)
        {
            LogMoveFailed(e, userId, channelId);
            return false;
        }
    }

    private async Task TryDeleteChannelAsync(ulong channelId)
    {
        try
        {
            await discord.DeleteChannelIfExistsAsync(channelId);
        }
        catch (Exception e)
        {
            LogChannelCleanupFailed(e, channelId);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Temp channel {ChannelId} created in guild {GuildId} for user {UserId}")]
    partial void LogCreated(ulong channelId, ulong guildId, ulong userId);

    [LoggerMessage(LogLevel.Debug, "Temp channel {ChannelId} in guild {GuildId} deleted (empty)")]
    partial void LogDeleted(ulong channelId, ulong guildId);

    [LoggerMessage(LogLevel.Debug, "Saved temp voice preferences for user {UserId} in guild {GuildId}")]
    partial void LogPreferencesSaved(ulong userId, ulong guildId);

    [LoggerMessage(LogLevel.Information, "Cleaned up {Count} stale temp channel(s) in guild {GuildId}")]
    partial void LogReconciled(ulong guildId, int count);

    [LoggerMessage(LogLevel.Warning, "Discord rejected saved temp voice preferences of user {UserId} in guild {GuildId}; using defaults")]
    partial void LogPreferencesRejected(Exception exception, ulong userId, ulong guildId);

    [LoggerMessage(LogLevel.Debug, "Couldn't move user {UserId} into temp channel {ChannelId}")]
    partial void LogMoveFailed(Exception exception, ulong userId, ulong channelId);

    [LoggerMessage(LogLevel.Warning, "Failed to delete channel {ChannelId} during temp voice cleanup")]
    partial void LogChannelCleanupFailed(Exception exception, ulong channelId);
}

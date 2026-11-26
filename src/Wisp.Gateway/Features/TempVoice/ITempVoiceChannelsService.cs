namespace Wisp.Gateway.Features.TempVoice;

/// <summary>
/// The lifecycle of temp channels: created when someone joins the master channel, deleted once empty. Calls about the
/// same owner are serialized, so overlapping gateway events can't give a user two channels.
/// </summary>
public interface ITempVoiceChannelsService
{
    /// <summary>
    /// Does something only when <paramref name="channelId"/> is an enabled master channel. Then the user is moved into
    /// their temp channel, which is created from their saved preferences if they don't have one yet.
    /// </summary>
    /// <param name="displayName">Used for the default channel name ("{name}'s channel").</param>
    /// <remarks>
    /// If the user has left voice by the time the channel is ready, it's deleted again rather than left empty.
    /// </remarks>
    Task UserJoinedAsync(
        ulong guildId,
        ulong userId,
        ulong channelId,
        string? displayName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes <paramref name="channelId"/> if it's a temp channel and nobody but <paramref name="userId"/> is left in
    /// it. The leaving user is ignored in case the gateway cache still lists them.
    /// </summary>
    Task UserLeftAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Saves what changed between <paramref name="before"/> and <paramref name="after"/> into the temp channel owner's
    /// preferences. Only changed settings are written, so a rename doesn't also pin the bitrate. Ignores channels that
    /// aren't temp channels.
    /// </summary>
    Task SavePreferencesAsync(
        ulong channelId,
        TempChannelSettings before,
        TempChannelSettings after,
        CancellationToken ct = default);
    
    /// <summary>
    /// Drops the record of a channel that's already gone from Discord.
    /// </summary>
    /// <returns><c>true</c> if it was a temp channel.</returns>
    Task<bool> ForgetChannelAsync(
        ulong channelId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes every temp channel in the guild, on Discord and in the database, including occupied ones.
    /// </summary>
    Task DeleteAllAsync(
        ulong guildId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Brings stored temp channels in line with the guild as it is now: records of channels that no longer exist are
    /// dropped, and empty channels are deleted.
    /// </summary>
    /// <param name="existingChannelIds">Every channel ID currently in the guild.</param>
    /// <param name="occupiedChannelIds">Channel IDs with at least one user in voice.</param>
    Task ReconcileAsync(
        ulong guildId,
        IReadOnlySet<ulong> existingChannelIds,
        IReadOnlySet<ulong> occupiedChannelIds,
        CancellationToken ct = default);
}

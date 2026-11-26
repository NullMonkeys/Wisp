namespace Wisp.Gateway.Features.TempVoice;

/// <summary>
/// Read-only view of the gateway cache that temp voice needs. No REST calls, so it's cheap but can be slightly stale.
/// </summary>
public interface ITempVoiceGuildState
{
    /// <summary>The bot's own user ID.</summary>
    ulong BotId { get; }
    
    bool ChannelExists(
        ulong guildId,
        ulong channelId);
    
    /// <returns>The channel's permission overwrites, or none if the channel isn't cached.</returns>
    IReadOnlyCollection<PermissionOverwriteSnapshot> GetPermissionOverwrites(
        ulong guildId,
        ulong channelId);
    
    /// <summary>
    /// Whether anyone other than <paramref name="exceptUserId"/> is in the voice channel. Returns <c>true</c> when the
    /// guild isn't cached, so a channel is never deleted on a guess.
    /// </summary>
    bool HasVoiceMembers(
        ulong guildId,
        ulong channelId,
        ulong exceptUserId);
}

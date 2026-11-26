using NetCord.Rest;

namespace Wisp.Gateway.Features.TempVoice;

/// <summary>
/// The Discord REST calls temp voice makes, kept behind an interface so tests can swap in a fake Discord.
/// </summary>
public interface ITempVoiceDiscord
{
    /// <returns>The new channel's ID.</returns>
    Task<ulong> CreateChannelAsync(
        ulong guildId,
        GuildChannelProperties channel,
        CancellationToken ct = default);
    
    /// <summary>
    /// Asks Discord directly rather than the gateway cache; see <see cref="ITempVoiceGuildState.ChannelExists"/> for
    /// the cheap version.
    /// </summary>
    Task<bool> ChannelExistsAsync(
        ulong channelId,
        CancellationToken ct = default);
    
    /// <returns><c>false</c> if the channel was already gone.</returns>
    Task<bool> DeleteChannelIfExistsAsync(
        ulong channelId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Moves a user who's in voice into another voice channel.
    /// </summary>
    /// <exception cref="RestException">Discord refused, for example because the user already left voice.</exception>
    Task MoveUserAsync(
        ulong guildId,
        ulong userId,
        ulong channelId,
        CancellationToken ct = default);
}
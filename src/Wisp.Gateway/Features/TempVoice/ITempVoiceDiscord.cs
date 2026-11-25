using NetCord.Rest;

namespace Wisp.Gateway.Features.TempVoice;

public interface ITempVoiceDiscord
{
    Task<ulong> CreateChannelAsync(
        ulong guildId,
        GuildChannelProperties channel,
        CancellationToken ct = default);
    
    Task<bool> ChannelExistsAsync(
        ulong channelId,
        CancellationToken ct = default);
    
    Task<bool> DeleteChannelIfExistsAsync(
        ulong channelId,
        CancellationToken ct = default);
    
    Task MoveUserAsync(
        ulong guildId,
        ulong userId,
        ulong channelId,
        CancellationToken ct = default);
}
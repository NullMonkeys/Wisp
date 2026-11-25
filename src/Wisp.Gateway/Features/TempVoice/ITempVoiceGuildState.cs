namespace Wisp.Gateway.Features.TempVoice;

public interface ITempVoiceGuildState
{
    ulong BotId { get; }
    
    bool ChannelExists(
        ulong guildId,
        ulong channelId);
    
    IReadOnlyCollection<PermissionOverwriteSnapshot> GetPermissionOverwrites(
        ulong guildId,
        ulong channelId);
    
    bool HasVoiceMembers(
        ulong guildId,
        ulong channelId,
        ulong exceptUserId);
}

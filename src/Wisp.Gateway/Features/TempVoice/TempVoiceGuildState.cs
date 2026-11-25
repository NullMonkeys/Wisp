using NetCord.Gateway;

namespace Wisp.Gateway.Features.TempVoice;

internal sealed class TempVoiceGuildState(ShardedGatewayClient client) : ITempVoiceGuildState
{
    public ulong BotId => client.Id;

    public bool ChannelExists(ulong guildId, ulong channelId)
        => GetGuild(guildId)?.Channels.ContainsKey(channelId) == true;

    public IReadOnlyCollection<PermissionOverwriteSnapshot> GetPermissionOverwrites(ulong guildId, ulong channelId)
    {
        var channel = GetGuild(guildId)?.Channels.GetValueOrDefault(channelId);
        return channel is null ? [] : PermissionOverwriteSnapshot.From(channel.PermissionOverwrites);
    }

    public bool HasVoiceMembers(ulong guildId, ulong channelId, ulong exceptUserId)
    {
        var guild = GetGuild(guildId);
        return guild is null || guild.VoiceStates.Values.Any(v => v.ChannelId == channelId && v.UserId != exceptUserId);
    }
    
    private Guild? GetGuild(ulong guildId)
        => client.GetShardForGuild(guildId).Cache.Guilds.GetValueOrDefault(guildId);
}
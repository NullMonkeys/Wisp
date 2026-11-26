using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

[UsedImplicitly]
public sealed class GuildChannelUpdate(ITempVoiceChannelsService channelsService) : IGuildChannelUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, IGuildChannel channel)
    {
        if (channel is not VoiceGuildChannel after)
            return;

        // The cache is updated only after handlers reach their first await, so it still holds the old channel.
        var guild = client.Cache.Guilds.GetValueOrDefault(channel.GuildId);
        if (guild?.Channels.GetValueOrDefault(channel.Id) is not VoiceGuildChannel before)
            return;

        await channelsService.SavePreferencesAsync(channel.Id, TempChannelSettings.From(before), TempChannelSettings.From(after));
    }
}

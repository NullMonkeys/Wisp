using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

/// <summary>
/// Remembers what the owner changed on their temp channel, so their next one comes out the same.
/// </summary>
/// <remarks>
/// The "before" state comes from the gateway cache. NetCord updates the cache once handlers reach their first
/// <c>await</c>, so it has to be read before awaiting anything.
/// </remarks>
[UsedImplicitly]
public sealed class GuildChannelUpdate(ITempVoiceChannelsService channelsService) : IGuildChannelUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, IGuildChannel channel)
    {
        if (channel is not VoiceGuildChannel after)
            return;

        var guild = client.Cache.Guilds.GetValueOrDefault(channel.GuildId);
        if (guild?.Channels.GetValueOrDefault(channel.Id) is not VoiceGuildChannel before)
            return;

        await channelsService.SavePreferencesAsync(channel.Id, TempChannelSettings.From(before), TempChannelSettings.From(after));
    }
}

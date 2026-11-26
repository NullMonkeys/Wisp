using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

/// <summary>
/// Turns a voice state change into "left a channel" and/or "joined a channel". Switching channels is both.
/// </summary>
/// <remarks>
/// The previous channel comes from the gateway cache. NetCord updates the cache once handlers reach their first
/// <c>await</c>, so it has to be read before awaiting anything.
/// </remarks>
[UsedImplicitly]
public sealed class VoiceStateUpdate(ITempVoiceChannelsService channelsService) : IVoiceStateUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, VoiceState state)
    {
        var oldChannelId = client.Cache.Guilds
            .GetValueOrDefault(state.GuildId)?
            .VoiceStates.GetValueOrDefault(state.UserId)?
            .ChannelId;

        var newChannelId = state.ChannelId;

        if (oldChannelId == newChannelId)
            return;
        
        if (oldChannelId is { } left)
            await channelsService.UserLeftAsync(state.GuildId, left, state.UserId);

        if (newChannelId is { } joined)
            await channelsService.UserJoinedAsync(state.GuildId, state.UserId, joined,
                state.User?.Nickname ?? state.User?.GlobalName ?? state.User?.Username);
    }
}

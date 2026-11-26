using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

[UsedImplicitly]
public sealed class VoiceStateUpdate(ITempVoiceChannelsService channelsService) : IVoiceStateUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, VoiceState state)
    {
        // NetCord applies the event to the cache only after handlers reach their first await,
        // so this still reads the user's previous channel. Keep it before any await.
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

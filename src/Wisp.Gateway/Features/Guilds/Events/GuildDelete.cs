using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.Guilds.Events;

/// <summary>
/// Forgets a guild once the bot is removed from it. Discord sends the same event when a guild goes unavailable during
/// an outage; that one is ignored so an outage doesn't wipe the guild's data.
/// </summary>
[UsedImplicitly]
public sealed class GuildDelete(IGuildsService guildsService) : IGuildDeleteShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildDeleteEventArgs arg)
    {
        if (arg.IsUnavailable) return;
        await guildsService.DeleteGuildAsync(arg.GuildId);
    }
}
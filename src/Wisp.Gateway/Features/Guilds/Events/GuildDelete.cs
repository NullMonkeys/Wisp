using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.Guilds.Events;

[UsedImplicitly]
public sealed class GuildDelete(IGuildsService guildsService) : IGuildDeleteShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildDeleteEventArgs arg)
    {
        if (arg.IsUnavailable) return;
        await guildsService.DeleteGuildAsync(arg.GuildId);
    }
}
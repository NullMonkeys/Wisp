using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.Guilds.Events;

[UsedImplicitly]
public sealed class GuildUpdate(IGuildsService guildsService) : IGuildUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, Guild arg)
    {
        await guildsService.EnsureGuildAsync(arg.Id, arg.Name);
    }
}
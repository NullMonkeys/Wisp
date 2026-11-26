using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.Guilds.Events;

/// <summary>
/// Saves the guild. GUILD_CREATE comes for every guild at the start of each gateway session, not only when the bot
/// joins one.
/// </summary>
[UsedImplicitly]
public sealed class GuildCreate(IGuildsService guildsService) : IGuildCreateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildCreateEventArgs arg)
    {
        ArgumentNullException.ThrowIfNull(arg.Guild);
        await guildsService.EnsureGuildAsync(arg.Guild.Id, arg.Guild.Name);
    }
}
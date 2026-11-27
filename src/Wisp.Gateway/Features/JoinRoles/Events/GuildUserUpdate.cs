using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.JoinRoles.Events;

[UsedImplicitly]
public sealed class GuildUserUpdate(IJoinRolesMembersService membersService) : IGuildUserUpdateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildUser user)
    {
        var wasPending = client.Cache.Guilds
            .GetValueOrDefault(user.GuildId)?
            .Users.GetValueOrDefault(user.Id)?
            .IsPending == true;

        await membersService.MemberUpdatedAsync(user.GuildId, user.Id, user.IsBot, wasPending, user.IsPending == true,
            [.. user.RoleIds]);
    }
}

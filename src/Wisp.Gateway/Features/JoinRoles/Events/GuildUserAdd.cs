using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.JoinRoles.Events;

[UsedImplicitly]
public sealed class GuildUserAdd(IJoinRolesMembersService membersService) : IGuildUserAddShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildUser user)
    {
        await membersService.MemberJoinedAsync(user.GuildId, user.Id, user.IsBot, user.IsPending == true, [.. user.RoleIds]);
    }
}

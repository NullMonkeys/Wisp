using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.JoinRoles.Events;

[UsedImplicitly]
public sealed class RoleDelete(IJoinRolesSettingsService settingsService) : IRoleDeleteShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, RoleDeleteEventArgs arg)
    {
        await settingsService.ForgetRoleAsync(arg.GuildId, arg.RoleId);
    }
}

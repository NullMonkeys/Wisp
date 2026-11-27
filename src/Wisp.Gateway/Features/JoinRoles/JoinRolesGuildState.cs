using NetCord;
using NetCord.Gateway;

namespace Wisp.Gateway.Features.JoinRoles;

internal sealed class JoinRolesGuildState(ShardedGatewayClient client) : IJoinRolesGuildState
{
    public RoleSnapshot? GetRole(ulong guildId, ulong roleId)
        => GetGuild(guildId)?.Roles.GetValueOrDefault(roleId) is { } role
            ? new RoleSnapshot(role.Id, role.RawPosition, role.Managed)
            : null;

    public BotSnapshot? GetBot(ulong guildId)
    {
        var guild = GetGuild(guildId);
        if (guild?.Users.GetValueOrDefault(client.Id) is not { } bot)
            return null;

        var roles = bot.RoleIds
            .Select(id => guild.Roles.GetValueOrDefault(id))
            .OfType<Role>()
            .ToList();

        var permissions = roles.Aggregate(
            guild.Roles.GetValueOrDefault(guild.Id)?.Permissions ?? default,
            (acc, role) => acc | role.Permissions);

        var canManageRoles = guild.OwnerId == client.Id
                             || permissions.HasFlag(Permissions.Administrator)
                             || permissions.HasFlag(Permissions.ManageRoles);

        return new BotSnapshot(roles.Count == 0 ? 0 : roles.Max(r => r.RawPosition), canManageRoles);
    }

    private Guild? GetGuild(ulong guildId)
        => client.GetShardForGuild(guildId).Cache.Guilds.GetValueOrDefault(guildId);
}

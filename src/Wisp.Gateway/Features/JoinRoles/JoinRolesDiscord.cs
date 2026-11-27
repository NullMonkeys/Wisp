using NetCord.Rest;

namespace Wisp.Gateway.Features.JoinRoles;

internal sealed class JoinRolesDiscord(RestClient restClient) : IJoinRolesDiscord
{
    public Task SetRolesAsync(ulong guildId, ulong userId, IReadOnlyCollection<ulong> roleIds, CancellationToken ct = default)
        => restClient.ModifyGuildUserAsync(guildId, userId, u => u.RoleIds = roleIds, cancellationToken: ct);
}

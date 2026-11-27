namespace Wisp.Gateway.Features.JoinRoles;

public interface IJoinRolesDiscord
{
    Task SetRolesAsync(
        ulong guildId,
        ulong userId,
        IReadOnlyCollection<ulong> roleIds,
        CancellationToken ct = default);
}

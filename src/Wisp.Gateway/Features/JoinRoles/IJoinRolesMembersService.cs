namespace Wisp.Gateway.Features.JoinRoles;

public interface IJoinRolesMembersService
{
    /// <summary>
    /// A member joined. Members still behind membership screening are skipped until they pass it, because giving
    /// them a role lets them bypass it.
    /// </summary>
    /// <param name="currentRoleIds">The member's roles as of the event; they're kept alongside the join roles.</param>
    /// <returns>How many roles were added.</returns>
    Task<int> MemberJoinedAsync(
        ulong guildId,
        ulong userId,
        bool isBot,
        bool isPending,
        IReadOnlyCollection<ulong> currentRoleIds,
        CancellationToken ct = default);

    /// <summary>
    /// A member changed. Gives them the join roles if this is the update where they passed membership screening.
    /// </summary>
    /// <param name="currentRoleIds">The member's roles as of the event; they're kept alongside the join roles.</param>
    /// <returns>How many roles were added.</returns>
    Task<int> MemberUpdatedAsync(
        ulong guildId,
        ulong userId,
        bool isBot,
        bool wasPending,
        bool isPending,
        IReadOnlyCollection<ulong> currentRoleIds,
        CancellationToken ct = default);
}

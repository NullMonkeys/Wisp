using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.JoinRoles;

public readonly record struct RoleSnapshot(ulong Id, int Position, bool Managed);

/// <param name="TopRolePosition">
/// Raw position of the bot's highest role. It can only assign roles strictly below it; on a tie Discord falls back to
/// comparing IDs, which we don't bother with, so a tied role is treated as not assignable.
/// </param>
public readonly record struct BotSnapshot(int TopRolePosition, bool CanManageRoles);

public enum RoleRejection
{
    Unknown,
    Everyone,
    Managed,
    AboveBot,
    OverLimit,
}

public readonly record struct RejectedRole(ulong RoleId, RoleRejection Reason);

public sealed record SetRolesResult(JoinRolesConfig Config, IReadOnlyList<RejectedRole> Rejected);

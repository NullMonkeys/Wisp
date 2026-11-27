using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

public sealed class JoinRolesConfig : Entity
{
    public const int MaxRoles = 25;

    public required ulong GuildId { get; init; }
    public bool Enabled { get; set; }

    public List<ulong> RoleIds { get; set; } = [];

    public Guild Guild { get; init; } = null!;
}

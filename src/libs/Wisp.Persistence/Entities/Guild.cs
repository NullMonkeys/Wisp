using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

public sealed class Guild : Entity<ulong>
{
    public required string Name { get; set; }
}

using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

public sealed class TempChannel : Entity
{
    public required ulong ChannelId { get; init; }
    public required ulong GuildId { get; init; }
    public required ulong OwnerId { get; set; }

    public Guild Guild { get; set; } = null!;
}
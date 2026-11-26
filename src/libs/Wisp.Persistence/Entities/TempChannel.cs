using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

/// <summary>
/// A temporary voice channel the bot created and still owns. A user owns at most one per guild.
/// </summary>
public sealed class TempChannel : Entity
{
    public required ulong ChannelId { get; init; }
    public required ulong GuildId { get; init; }
    public required ulong OwnerId { get; set; }

    public Guild Guild { get; set; } = null!;
}
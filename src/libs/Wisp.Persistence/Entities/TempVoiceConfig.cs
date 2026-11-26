using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

/// <summary>
/// A guild's temp voice setup. While <see cref="Enabled"/> is set, both channel IDs are set too (enforced by a check
/// constraint); disabling clears them.
/// </summary>
public sealed class TempVoiceConfig : Entity
{
    public required ulong GuildId { get; init; }
    public bool Enabled { get; set; }
    
    /// <summary>The category the bot created; temp channels are created inside it and copy its overwrites.</summary>
    public ulong? CategoryId  { get; set; }

    /// <summary>The "join to create" voice channel. Joining it gives the user their own temp channel.</summary>
    public ulong? MasterChannelId { get; set; }

    public Guild Guild { get; init; } = null!;
}
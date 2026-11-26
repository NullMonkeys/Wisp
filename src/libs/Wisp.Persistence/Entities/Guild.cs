using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

/// <summary>
/// A Discord server the bot is in, keyed by its snowflake ID. Every other guild-scoped row cascades from it, so
/// deleting a guild wipes all of its data.
/// </summary>
public sealed class Guild : Entity<ulong>
{
    public required string Name { get; set; }
}

using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

/// <summary>
/// Mirrors NetCord's <c>PermissionOverwriteType</c> (and Discord's wire values), so the two are cast back and forth.
/// Don't renumber.
/// </summary>
public enum PermissionOverwriteKind
{
    Role = 0,
    User = 1,
}

/// <summary>
/// What a user changed on their temp channel, remembered per guild and applied to the next one they get.
/// A <c>null</c> setting means "never changed, use the default".
/// </summary>
public sealed class TempVoicePreferences : Entity
{
    public required ulong GuildId { get; init; }
    public required ulong UserId { get; init; }
    
    public string? Name { get; set; }
    public int? UserLimit { get; set; }
    public int? Bitrate { get; set; }

    /// <summary>
    /// Overwrites the user added on top of the category's, stored as JSON. Ones identical to the category's, and the
    /// owner's and bot's own, aren't kept.
    /// </summary>
    public ICollection<TempVoiceOverwrite> Overwrites { get; set; } = new List<TempVoiceOverwrite>();
}

/// <summary>
/// A stored permission overwrite. <see cref="Allowed"/> and <see cref="Denied"/> are raw Discord permission bit flags.
/// </summary>
public sealed class TempVoiceOverwrite
{
    /// <summary>The role or user ID the overwrite applies to, depending on <see cref="Kind"/>.</summary>
    public required ulong Id { get; init; }
    public required PermissionOverwriteKind Kind { get; init; }
    public ulong Allowed { get; init; }
    public ulong Denied { get; init; }
}
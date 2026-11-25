using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

public enum PermissionOverwriteKind
{
    Role = 0,
    User = 1,
}

public sealed class TempVoicePreferences : Entity
{
    public required ulong GuildId { get; init; }
    public required ulong UserId { get; init; }
    
    public string? Name { get; set; }
    public int? UserLimit { get; set; }
    public int? Bitrate { get; set; }
    public ICollection<TempVoiceOverwrite> Overwrites { get; set; } = new List<TempVoiceOverwrite>();
}

public sealed class TempVoiceOverwrite
{
    public required ulong Id { get; init; }
    public required PermissionOverwriteKind Kind { get; init; }
    public ulong Allowed { get; init; }
    public ulong Denied { get; init; }
}
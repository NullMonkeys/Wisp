using Wisp.Persistence.Common;

namespace Wisp.Persistence.Entities;

public sealed class TempVoiceConfig : Entity
{
    public required ulong GuildId { get; init; }
    public bool Enabled { get; set; }
    
    public ulong? CategoryId  { get; set; }
    public ulong? MasterChannelId { get; set; }

    public Guild Guild { get; init; } = null!;
}
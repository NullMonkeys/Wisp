using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice;

public interface ITempVoiceSettingsService
{
    Task<TempVoiceConfig> GetAsync(
        ulong guildId,
        CancellationToken ct = default);
    
    Task<TempVoiceConfig> EnableAsync(
        ulong guildId,
        CancellationToken ct = default);
    
    Task<TempVoiceConfig> DisableAsync(
        ulong guildId,
        CancellationToken ct = default);
}
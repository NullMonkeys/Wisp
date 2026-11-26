using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice;

/// <summary>
/// Turns temp voice on and off for a guild. Enable and disable calls for the same guild run one at a time.
/// </summary>
public interface ITempVoiceSettingsService
{
    /// <summary>
    /// Returns the guild's config, or a disabled one (not saved) if it never had one.
    /// </summary>
    Task<TempVoiceConfig> GetAsync(
        ulong guildId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Creates the "Temporary Voice" category and its master channel. If temp voice is already enabled and both still
    /// exist, nothing happens.
    /// </summary>
    /// <param name="force">Recreate the setup even if it looks fine.</param>
    /// <remarks>
    /// Recreating deletes existing temp channels. The old category and master channel are removed only after the new
    /// ones are saved, so a failure never leaves the config pointing at deleted channels.
    /// </remarks>
    Task<TempVoiceConfig> EnableAsync(
        ulong guildId,
        bool force,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes every temp channel, the master channel and the category. Does nothing if temp voice isn't enabled.
    /// </summary>
    Task<TempVoiceConfig> DisableAsync(
        ulong guildId,
        CancellationToken ct = default);
}
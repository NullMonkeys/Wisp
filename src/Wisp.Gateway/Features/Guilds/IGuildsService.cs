namespace Wisp.Gateway.Features.Guilds;

public interface IGuildsService
{
    /// <summary>
    /// Saves the guild, or updates its name. Writes nothing when the stored name is already current, so it's cheap to
    /// call on every GUILD_CREATE.
    /// </summary>
    Task EnsureGuildAsync(ulong id, string name, CancellationToken ct = default);

    /// <summary>
    /// Deletes the guild and, through cascades, everything stored for it (temp voice setup, channels, preferences).
    /// It doesn't touch anything on Discord.
    /// </summary>
    Task DeleteGuildAsync(ulong id, CancellationToken ct = default);
}
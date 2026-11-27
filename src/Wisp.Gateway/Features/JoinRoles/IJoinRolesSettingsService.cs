using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.JoinRoles;

public interface IJoinRolesSettingsService
{
    Task<JoinRolesConfig> GetAsync(
        ulong guildId,
        CancellationToken ct = default);

    /// <summary>
    /// Turns join roles on. Does nothing if no roles are configured, so check <see cref="JoinRolesConfig.Enabled"/>.
    /// </summary>
    Task<JoinRolesConfig> EnableAsync(
        ulong guildId,
        CancellationToken ct = default);

    /// <summary>
    /// Turns join roles off and keeps the configured roles, so enabling again doesn't mean picking them again.
    /// </summary>
    Task<JoinRolesConfig> DisableAsync(
        ulong guildId,
        CancellationToken ct = default);

    /// <summary>
    /// Replaces the configured roles with the ones the bot can actually assign, and reports the rest.
    /// </summary>
    Task<SetRolesResult> SetRolesAsync(
        ulong guildId,
        IReadOnlyCollection<ulong> roleIds,
        CancellationToken ct = default);

    /// <summary>
    /// Drops a role that was deleted from the guild.
    /// </summary>
    Task ForgetRoleAsync(
        ulong guildId,
        ulong roleId,
        CancellationToken ct = default);
}

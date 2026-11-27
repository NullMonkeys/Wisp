using Microsoft.EntityFrameworkCore;

using Wisp.Gateway.Common;
using Wisp.Persistence;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.JoinRoles;

internal sealed partial class JoinRolesSettingsService(
    ILogger<JoinRolesSettingsService> logger,
    IJoinRolesGuildState guildState,
    WispDbContext dbContext) : IJoinRolesSettingsService
{
    private static readonly KeyedLock<ulong> GuildLocks = new();

    public async Task<JoinRolesConfig> GetAsync(ulong guildId, CancellationToken ct = default)
    {
        return await dbContext
                   .JoinRolesConfigs
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.GuildId == guildId, ct)
               ?? new JoinRolesConfig { GuildId = guildId };
    }

    public async Task<JoinRolesConfig> EnableAsync(ulong guildId, CancellationToken ct = default)
    {
        using var _ = await GuildLocks.AcquireAsync(guildId, ct);

        var config = await dbContext.JoinRolesConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (config is null or { Enabled: true } || config.RoleIds.Count == 0)
            return config ?? new JoinRolesConfig { GuildId = guildId };

        config.Enabled = true;
        await dbContext.SaveChangesAsync(ct);

        LogEnabled(guildId, config.RoleIds.Count);
        return config;
    }

    public async Task<JoinRolesConfig> DisableAsync(ulong guildId, CancellationToken ct = default)
    {
        using var _ = await GuildLocks.AcquireAsync(guildId, ct);

        var config = await dbContext.JoinRolesConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (config is not { Enabled: true })
            return config ?? new JoinRolesConfig { GuildId = guildId };

        config.Enabled = false;
        await dbContext.SaveChangesAsync(ct);

        LogDisabled(guildId);
        return config;
    }

    public async Task<SetRolesResult> SetRolesAsync(ulong guildId, IReadOnlyCollection<ulong> roleIds, CancellationToken ct = default)
    {
        (List<ulong> accepted, List<RejectedRole> rejected) = Validate(guildId, roleIds);

        using var _ = await GuildLocks.AcquireAsync(guildId, ct);

        var config = await dbContext.JoinRolesConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (config is null)
        {
            config = new JoinRolesConfig { GuildId = guildId };
            dbContext.JoinRolesConfigs.Add(config);
        }

        config.RoleIds = accepted;
        await dbContext.SaveChangesAsync(ct);

        LogRolesSet(guildId, accepted.Count, rejected.Count);
        return new SetRolesResult(config, rejected);
    }

    public async Task ForgetRoleAsync(ulong guildId, ulong roleId, CancellationToken ct = default)
    {
        using var _ = await GuildLocks.AcquireAsync(guildId, ct);

        var config = await dbContext.JoinRolesConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (config is null || !config.RoleIds.Contains(roleId))
            return;

        config.RoleIds = [.. config.RoleIds.Where(id => id != roleId)];
        await dbContext.SaveChangesAsync(ct);

        LogRoleForgotten(guildId, roleId);
    }

    private (List<ulong> Accepted, List<RejectedRole> Rejected) Validate(ulong guildId, IReadOnlyCollection<ulong> roleIds)
    {
        var bot = guildState.GetBot(guildId);
        List<ulong> accepted = [];
        List<RejectedRole> rejected = [];

        foreach (var roleId in roleIds.Distinct())
        {
            var reason = roleId == guildId
                ? RoleRejection.Everyone
                : guildState.GetRole(guildId, roleId) switch
                {
                    null => RoleRejection.Unknown,
                    { Managed: true } => RoleRejection.Managed,
                    var role when bot is null || role.Value.Position >= bot.Value.TopRolePosition => RoleRejection.AboveBot,
                    _ when accepted.Count >= JoinRolesConfig.MaxRoles => RoleRejection.OverLimit,
                    _ => (RoleRejection?)null,
                };

            if (reason is { } r)
                rejected.Add(new RejectedRole(roleId, r));
            else
                accepted.Add(roleId);
        }

        return (accepted, rejected);
    }

    [LoggerMessage(LogLevel.Information, "Join roles enabled in guild {GuildId} with {RoleCount} roles")]
    partial void LogEnabled(ulong guildId, int roleCount);

    [LoggerMessage(LogLevel.Information, "Join roles disabled in guild {GuildId}")]
    partial void LogDisabled(ulong guildId);

    [LoggerMessage(LogLevel.Information, "Join roles in guild {GuildId} set to {AcceptedCount} roles ({RejectedCount} rejected)")]
    partial void LogRolesSet(ulong guildId, int acceptedCount, int rejectedCount);

    [LoggerMessage(LogLevel.Information, "Deleted role {RoleId} removed from join roles in guild {GuildId}")]
    partial void LogRoleForgotten(ulong guildId, ulong roleId);
}

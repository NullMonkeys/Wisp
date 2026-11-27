using Microsoft.EntityFrameworkCore;

using NetCord.Rest;

using Wisp.Persistence;

namespace Wisp.Gateway.Features.JoinRoles;

internal sealed partial class JoinRolesMembersService(
    ILogger<JoinRolesMembersService> logger,
    IJoinRolesDiscord discord,
    IJoinRolesGuildState guildState,
    WispDbContext dbContext) : IJoinRolesMembersService
{
    public Task<int> MemberJoinedAsync(ulong guildId, ulong userId, bool isBot, bool isPending,
        IReadOnlyCollection<ulong> currentRoleIds, CancellationToken ct = default)
        => isBot || isPending ? Task.FromResult(0) : AssignAsync(guildId, userId, currentRoleIds, ct);

    public Task<int> MemberUpdatedAsync(ulong guildId, ulong userId, bool isBot, bool wasPending, bool isPending,
        IReadOnlyCollection<ulong> currentRoleIds, CancellationToken ct = default)
        => isBot || !wasPending || isPending ? Task.FromResult(0) : AssignAsync(guildId, userId, currentRoleIds, ct);

    /// <summary>
    /// Gives every join role in one call, which replaces the member's whole role list. A role another bot adds at
    /// the same moment can be lost; running two join-role bots is a server misconfiguration we don't design around.
    /// </summary>
    private async Task<int> AssignAsync(ulong guildId, ulong userId, IReadOnlyCollection<ulong> currentRoleIds,
        CancellationToken ct)
    {
        var config = await dbContext.JoinRolesConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId, ct);

        if (config is not { Enabled: true })
            return 0;

        // Deleted roles are pruned on GUILD_ROLE_DELETE, but that may not have landed yet, and one unknown role
        // would fail the whole call.
        var added = config.RoleIds
            .Where(id => !currentRoleIds.Contains(id) && guildState.GetRole(guildId, id) is not null)
            .ToList();

        if (added.Count == 0)
            return 0;

        try
        {
            await discord.SetRolesAsync(guildId, userId, [.. currentRoleIds, .. added], ct);
            return added.Count;
        }
        catch (RestException e)
        {
            // Usually a role was moved above the bot or the bot lost Manage Roles.
            LogAssignFailed(e, userId, guildId);
            return 0;
        }
    }

    [LoggerMessage(LogLevel.Warning, "Failed to give join roles to user {UserId} in guild {GuildId}")]
    partial void LogAssignFailed(Exception exception, ulong userId, ulong guildId);
}

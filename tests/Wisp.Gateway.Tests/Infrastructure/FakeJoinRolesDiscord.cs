using System.Net;

using NetCord.Rest;

using Wisp.Gateway.Features.JoinRoles;

namespace Wisp.Gateway.Tests.Infrastructure;

/// <summary>
/// One guild's roles and the bot's place among them, plus every role the bot hands out.
/// </summary>
public sealed class FakeJoinRolesDiscord : IJoinRolesDiscord, IJoinRolesGuildState
{
    private readonly Lock _lock = new();
    private readonly Dictionary<ulong, RoleSnapshot> _roles = [];
    private readonly HashSet<ulong> _failingRoles = [];
    private readonly Dictionary<ulong, IReadOnlyList<ulong>> _memberRoles = [];
    private int _calls;

    public FakeJoinRolesDiscord(ulong guildId)
    {
        GuildId = guildId;
        _roles[guildId] = new RoleSnapshot(guildId, 0, Managed: false); // @everyone
    }

    public ulong GuildId { get; }

    /// <summary>
    /// The bot's member. <c>null</c> means it isn't cached.
    /// </summary>
    public BotSnapshot? Bot { get; set; } = new(TopRolePosition: 100, CanManageRoles: true);

    #region Arranging

    public ulong AddRole(int position = 1, bool managed = false)
    {
        var id = TestIds.Next();
        lock (_lock)
            _roles[id] = new RoleSnapshot(id, position, managed);
        return id;
    }

    public void DeleteRole(ulong roleId)
    {
        lock (_lock)
            _roles.Remove(roleId);
    }

    /// <summary>
    /// Any role update that includes this role fails, as if it had been moved above the bot.
    /// </summary>
    public void FailRole(ulong roleId)
    {
        lock (_lock)
            _failingRoles.Add(roleId);
    }

    #endregion

    #region Inspecting

    public IReadOnlyList<ulong> RolesOf(ulong userId)
    {
        lock (_lock)
            return _memberRoles.GetValueOrDefault(userId) ?? [];
    }

    /// <summary>
    /// How many role updates were sent, failed ones included.
    /// </summary>
    public int Calls
    {
        get { lock (_lock) return _calls; }
    }

    #endregion

    public Task SetRolesAsync(ulong guildId, ulong userId, IReadOnlyCollection<ulong> roleIds, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _calls++;

            if (roleIds.Any(_failingRoles.Contains))
                return Task.FromException(new RestException(HttpStatusCode.Forbidden, "Missing Permissions"));

            _memberRoles[userId] = [.. roleIds];
            return Task.CompletedTask;
        }
    }

    public RoleSnapshot? GetRole(ulong guildId, ulong roleId)
    {
        lock (_lock)
            return guildId == GuildId && _roles.TryGetValue(roleId, out var role) ? role : null;
    }

    public BotSnapshot? GetBot(ulong guildId) => guildId == GuildId ? Bot : null;
}

using System.Net;

using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Features.TempVoice;

namespace Wisp.Gateway.Tests.Infrastructure;

/// <summary>
/// An in-memory Discord for one test: channels, who's in voice, and REST failures on demand. It plays both the REST
/// side and the gateway cache, so the two always agree.
/// </summary>
public sealed class FakeDiscord : ITempVoiceDiscord, ITempVoiceGuildState
{
    private readonly Lock _lock = new();
    private readonly Dictionary<ulong, FakeChannel> _channels = [];
    private readonly Dictionary<(ulong GuildId, ulong UserId), ulong> _voice = [];
    private readonly List<(Func<GuildChannelProperties, bool> When, Exception Exception)> _createFailures = [];
    private int _createdCount;

    public ulong BotId { get; } = TestIds.Next();

    public bool FailMoves { get; set; }

    public int CreatedCount
    {
        get { lock (_lock) return _createdCount; }
    }

    #region Arranging

    /// <summary>
    /// Makes the next channel creation that matches <paramref name="when"/> (or any, if omitted) throw
    /// <paramref name="exception"/>. Each call fails one creation.
    /// </summary>
    public void FailNextCreate(Exception exception, Func<GuildChannelProperties, bool>? when = null)
    {
        lock (_lock)
            _createFailures.Add((when ?? (_ => true), exception));
    }

    public void SetOverwrites(ulong channelId, params PermissionOverwriteSnapshot[] overwrites)
    {
        lock (_lock)
            _channels[channelId] = _channels[channelId] with { Overwrites = overwrites };
    }

    public void DeleteChannel(ulong channelId)
    {
        lock (_lock)
            RemoveChannel(channelId);
    }

    public void Connect(ulong guildId, ulong userId, ulong channelId)
    {
        lock (_lock)
            _voice[(guildId, userId)] = channelId;
    }

    public void Disconnect(ulong guildId, ulong userId)
    {
        lock (_lock)
            _voice.Remove((guildId, userId));
    }

    #endregion

    #region Inspecting

    public FakeChannel? Channel(ulong channelId)
    {
        lock (_lock)
            return _channels.GetValueOrDefault(channelId);
    }

    public IReadOnlyList<FakeChannel> ChannelsIn(ulong guildId)
    {
        lock (_lock)
            return _channels.Values.Where(c => c.GuildId == guildId).ToList();
    }

    public ulong? ChannelOf(ulong guildId, ulong userId)
    {
        lock (_lock)
            return _voice.TryGetValue((guildId, userId), out var channelId) ? channelId : null;
    }

    public HashSet<ulong> ChannelIds(ulong guildId)
    {
        lock (_lock)
            return [.. _channels.Values.Where(c => c.GuildId == guildId).Select(c => c.Id)];
    }

    public HashSet<ulong> OccupiedChannelIds(ulong guildId)
    {
        lock (_lock)
            return [.. _voice.Where(v => v.Key.GuildId == guildId).Select(v => v.Value)];
    }

    /// <summary>
    /// The channel's settings as Discord would report them, with Discord's defaults (no user limit, 64 kbps) filled in.
    /// </summary>
    public TempChannelSettings SettingsOf(ulong channelId)
    {
        var channel = Channel(channelId) ?? throw new InvalidOperationException($"Channel {channelId} doesn't exist.");
        return new TempChannelSettings(channel.Name, channel.UserLimit ?? 0, channel.Bitrate ?? 64000, channel.ParentId,
            channel.Overwrites);
    }

    #endregion

    #region ITempVoiceDiscord

    public Task<ulong> CreateChannelAsync(ulong guildId, GuildChannelProperties channel, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var failure = _createFailures.FindIndex(f => f.When(channel));
            if (failure >= 0)
            {
                var exception = _createFailures[failure].Exception;
                _createFailures.RemoveAt(failure);
                return Task.FromException<ulong>(exception);
            }

            var id = TestIds.Next();
            var overwrites = channel.PermissionOverwrites?
                .Select(o => new PermissionOverwriteSnapshot(o.Id, o.Type, o.Allowed ?? default, o.Denied ?? default))
                .ToArray() ?? [];

            _channels[id] = new FakeChannel(guildId, id, channel.Name, channel.Type, channel.ParentId, overwrites,
                channel.UserLimit, channel.Bitrate);
            _createdCount++;

            return Task.FromResult(id);
        }
    }

    public Task<bool> ChannelExistsAsync(ulong channelId, CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult(_channels.ContainsKey(channelId));
    }

    public Task<bool> DeleteChannelIfExistsAsync(ulong channelId, CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult(RemoveChannel(channelId));
    }

    public Task MoveUserAsync(ulong guildId, ulong userId, ulong channelId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (FailMoves || !_voice.ContainsKey((guildId, userId)))
                return Task.FromException(new RestException(HttpStatusCode.BadRequest, "Target user is not connected to voice."));

            if (!_channels.ContainsKey(channelId))
                return Task.FromException(new RestException(HttpStatusCode.NotFound, "Unknown Channel"));

            _voice[(guildId, userId)] = channelId;
            return Task.CompletedTask;
        }
    }

    #endregion

    #region ITempVoiceGuildState

    public bool ChannelExists(ulong guildId, ulong channelId)
    {
        lock (_lock)
            return _channels.TryGetValue(channelId, out var channel) && channel.GuildId == guildId;
    }

    public IReadOnlyCollection<PermissionOverwriteSnapshot> GetPermissionOverwrites(ulong guildId, ulong channelId)
    {
        lock (_lock)
            return _channels.TryGetValue(channelId, out var channel) ? channel.Overwrites : [];
    }

    public bool HasVoiceMembers(ulong guildId, ulong channelId, ulong exceptUserId)
    {
        lock (_lock)
            return _voice.Any(v => v.Key.GuildId == guildId && v.Value == channelId && v.Key.UserId != exceptUserId);
    }

    #endregion

    private bool RemoveChannel(ulong channelId)
    {
        if (!_channels.Remove(channelId))
            return false;

        foreach (var member in _voice.Where(v => v.Value == channelId).Select(v => v.Key).ToList())
            _voice.Remove(member);

        return true;
    }
}

public sealed record FakeChannel(
    ulong GuildId,
    ulong Id,
    string Name,
    ChannelType Type,
    ulong? ParentId,
    IReadOnlyList<PermissionOverwriteSnapshot> Overwrites,
    int? UserLimit,
    int? Bitrate)
{
    public PermissionOverwriteSnapshot? OverwriteFor(ulong id)
        => Overwrites.Cast<PermissionOverwriteSnapshot?>().FirstOrDefault(o => o!.Value.Id == id);
}

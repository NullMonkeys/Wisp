using NetCord;

namespace Wisp.Gateway.Features.TempVoice;

public readonly record struct PermissionOverwriteSnapshot(
    ulong Id,
    PermissionOverwriteType Type,
    Permissions Allowed,
    Permissions Denied)
{
    public static PermissionOverwriteSnapshot From(PermissionOverwrite overwrite)
        => new(overwrite.Id, overwrite.Type, overwrite.Allowed, overwrite.Denied);

    public static PermissionOverwriteSnapshot[] From(IReadOnlyDictionary<ulong, PermissionOverwrite> overwrites)
        => [.. overwrites.Values.Select(From)];
}

public sealed record TempChannelSettings(
    string Name,
    int UserLimit,
    int Bitrate,
    ulong? CategoryId,
    IReadOnlyCollection<PermissionOverwriteSnapshot> Overwrites)
{
    public static TempChannelSettings From(VoiceGuildChannel channel)
        => new(channel.Name, channel.UserLimit, channel.Bitrate, channel.ParentId,
            PermissionOverwriteSnapshot.From(channel.PermissionOverwrites));
}

public enum TransferStatus
{
    Transferred,

    /// <summary>The caller doesn't own a temp channel in this guild.</summary>
    NotOwner,

    /// <summary>The caller tried to hand their channel to themselves.</summary>
    TargetIsOwner,

    /// <summary>The new owner isn't connected to the channel being handed over.</summary>
    TargetNotInChannel,

    /// <summary>The new owner already owns another temp channel; nobody can own two.</summary>
    TargetOwnsChannel,
}

public readonly record struct TransferResult(TransferStatus Status, ulong? ChannelId = null);

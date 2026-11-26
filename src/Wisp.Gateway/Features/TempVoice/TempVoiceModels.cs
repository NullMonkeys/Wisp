using NetCord;

namespace Wisp.Gateway.Features.TempVoice;

/// <summary>
/// A plain copy of a NetCord <see cref="PermissionOverwrite"/>. NetCord compares overwrites by ID only; this compares
/// every field, which is what lets two sets of overwrites be compared to spot permission changes.
/// </summary>
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

/// <summary>
/// The parts of a voice channel an owner can change and that get saved as their preferences.
/// </summary>
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

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

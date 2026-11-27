using NetCord;
using NetCord.Rest;

using Wisp.Discord.Components;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice.Modules;

internal static class TempVoiceCards
{
    public const string EnableId = "temp-voice-enable";
    public const string DisableId = "temp-voice-disable";

    public static ComponentContainerProperties Settings(TempVoiceConfig config, int activeChannels)
    {
        var status = config.Enabled
            ? $"**Status:** enabled · {activeChannels} active {(activeChannels == 1 ? "channel" : "channels")}"
            : "**Status:** disabled";

        var detail = config is { Enabled: true, MasterChannelId: { } masterChannelId }
            ? $"Join <#{masterChannelId}> to get your own channel."
            : "When enabled, I'll create a category with a channel members can join to get their own.";

        return new ComponentContainerProperties
        {
            AccentColor = config.Enabled ? Color.Success : null,
            Components =
            [
                new TextDisplayProperties("### 🔊 Temporary voice"),
                Cards.StatusSection(config.Enabled, status, EnableId, DisableId),
                new TextDisplayProperties(detail),
                new ComponentSeparatorProperties(),
                new TextDisplayProperties("-# Channels disappear once everyone leaves, and remember their owner's settings."),
            ],
        };
    }

    public static ComponentContainerProperties Transfer(TransferResult result, ulong targetId) => result.Status switch
    {
        TransferStatus.Transferred
            => Cards.Success("Channel handed over", $"<#{result.ChannelId}> now belongs to <@{targetId}>."),
        TransferStatus.NotOwner
            => Cards.Warning("Can't do that", "You don't own a temporary voice channel."),
        TransferStatus.TargetIsOwner
            => Cards.Warning("Can't do that", "Pick someone else in your channel to hand it over to."),
        TransferStatus.TargetNotInChannel
            => Cards.Warning("Can't do that", $"<@{targetId}> needs to be in <#{result.ChannelId}> first."),
        TransferStatus.TargetOwnsChannel
            => Cards.Warning("Can't do that", $"<@{targetId}> already has their own channel."),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
    };

    public static ComponentContainerProperties BotCantOwn()
        => Cards.Warning("Can't do that", "Bots can't own channels.");
}

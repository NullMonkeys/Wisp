using NetCord;
using NetCord.Rest;

using Wisp.Discord.Components;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice.Commands;

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
}

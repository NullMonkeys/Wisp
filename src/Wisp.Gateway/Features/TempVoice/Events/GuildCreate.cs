using JetBrains.Annotations;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

/// <summary>
/// Catches up on whatever happened while the bot was offline: disables temp voice if its master channel or category is
/// gone, and cleans up temp channels that were deleted or emptied in the meantime.
/// </summary>
[UsedImplicitly]
public sealed class GuildCreate(
    ITempVoiceChannelsService channelsService,
    ITempVoiceSettingsService settingsService) : IGuildCreateShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, GuildCreateEventArgs arg)
    {
        if (arg.Guild is not { } guild)
            return;

        var config = await settingsService.GetAsync(guild.Id);
        if (!config.Enabled)
            return;

        if (!guild.Channels.ContainsKey(config.MasterChannelId!.Value) || !guild.Channels.ContainsKey(config.CategoryId!.Value))
        {
            await settingsService.DisableAsync(guild.Id);
            return;
        }

        var occupiedChannelIds = guild.VoiceStates.Values
            .Where(v => v.ChannelId.HasValue)
            .Select(v => v.ChannelId!.Value)
            .ToHashSet();

        await channelsService.ReconcileAsync(guild.Id, guild.Channels.Keys.ToHashSet(), occupiedChannelIds);
    }
}

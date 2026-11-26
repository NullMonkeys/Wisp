using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

/// <summary>
/// Keeps the database in sync when a channel is deleted on Discord. A temp channel is just forgotten; if it was the
/// master channel or the category, temp voice can't work anymore and is disabled for the guild.
/// </summary>
[UsedImplicitly]
public sealed class GuildChannelDelete(
    ITempVoiceChannelsService channelsService,
    ITempVoiceSettingsService settingsService) : IGuildChannelDeleteShardedGatewayHandler
{
    public async ValueTask HandleAsync(GatewayClient client, IGuildChannel channel)
    {
        if (await channelsService.ForgetChannelAsync(channel.Id))
            return;

        var config = await settingsService.GetAsync(channel.GuildId);
        if (config.Enabled && (config.MasterChannelId == channel.Id || config.CategoryId == channel.Id))
            await settingsService.DisableAsync(channel.GuildId);
    }
}

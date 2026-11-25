using JetBrains.Annotations;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Wisp.Gateway.Features.TempVoice.Events;

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

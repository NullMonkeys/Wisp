using JetBrains.Annotations;

using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord.Interactions;

namespace Wisp.Gateway.Features.TempVoice.Commands;

[UsedImplicitly]
[RequireUserPermissions<ApplicationCommandContext>(Permissions.Administrator)]
public class TempVoiceCommandsModule(
    ITempVoiceSettingsService settingsService,
    ITempVoiceChannelsService channelsService) : WispCommandModule
{
    [UsedImplicitly]
    [SlashCommand("temp-voice", "Temporary voice channels",
        Contexts = [InteractionContextType.Guild],
        DefaultGuildPermissions = Permissions.Administrator)]
    public async Task ShowTempVoice()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var guildId = Context.Guild.Id;
        await DeferAsync();

        var config = await settingsService.GetAsync(guildId);
        var activeChannels = await channelsService.CountActiveAsync(guildId);
        await ReplyAsync(TempVoiceCards.Settings(config, activeChannels));
    }
}

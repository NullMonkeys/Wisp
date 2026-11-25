using JetBrains.Annotations;

using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord;

namespace Wisp.Gateway.Features.TempVoice.Commands;

[UsedImplicitly]
[RequireUserPermissions<ApplicationCommandContext>(Permissions.Administrator)]
[SlashCommand("temp-voice", "Temporary voice channels", Contexts = [InteractionContextType.Guild])]
public class TempVoiceCommandsModule(ITempVoiceSettingsService settingsService) : WispCommandModule
{
    [UsedImplicitly]
    [SubSlashCommand("enable", "Enable Temp voice channels")]
    public async Task EnableTempVoiceChannels()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        await DeferAsync();
        await settingsService.EnableAsync(Context.Guild.Id, force: false);
        await ReplyAsync(Cards.Success("Temp voice channels enabled"));
    }

    [UsedImplicitly]
    [SubSlashCommand("disable", "Disable Temp voice channels")]
    public async Task DisableTempVoiceChannels()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        await DeferAsync();
        await settingsService.DisableAsync(Context.Guild.Id);
        await ReplyAsync(Cards.Success("Temp voice channels disabled"));
    }
}
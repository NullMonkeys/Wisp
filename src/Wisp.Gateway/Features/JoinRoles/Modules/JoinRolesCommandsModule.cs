using JetBrains.Annotations;

using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord.Interactions;

namespace Wisp.Gateway.Features.JoinRoles.Modules;

[UsedImplicitly]
[RequireUserPermissions<ApplicationCommandContext>(Permissions.Administrator)]
public class JoinRolesCommandsModule(
    IJoinRolesSettingsService settingsService,
    IJoinRolesGuildState guildState) : WispCommandModule
{
    [UsedImplicitly]
    [SlashCommand("join-roles", "Roles given to new members",
        Contexts = [InteractionContextType.Guild],
        DefaultGuildPermissions = Permissions.Administrator)]
    public async Task ShowJoinRoles()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var guildId = Context.Guild.Id;
        await DeferAsync();

        var config = await settingsService.GetAsync(guildId);
        await ReplyAsync(JoinRolesCards.Settings(config, guildState.GetBot(guildId)));
    }
}

using JetBrains.Annotations;

using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.JoinRoles.Commands;

/// <summary>
/// The Enable / Disable button on the <c>/join-roles</c> card. Both only touch the database, so they redraw the
/// card straight away.
/// </summary>
[UsedImplicitly]
[RequireUserPermissions<ButtonInteractionContext>(Permissions.Administrator)]
public class JoinRolesButtonsModule(
    IJoinRolesSettingsService settingsService,
    IJoinRolesGuildState guildState) : ComponentInteractionModule<ButtonInteractionContext>
{
    [UsedImplicitly]
    [ComponentInteraction(JoinRolesCards.EnableId)]
    public Task Enable() => UpdateAsync(settingsService.EnableAsync);

    [UsedImplicitly]
    [ComponentInteraction(JoinRolesCards.DisableId)]
    public Task Disable() => UpdateAsync(settingsService.DisableAsync);

    private async Task UpdateAsync(Func<ulong, CancellationToken, Task<JoinRolesConfig>> action)
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var guildId = Context.Guild.Id;

        // enable does nothing if the roles were cleared from another card; the redraw then shows it disabled
        var config = await action(guildId, CancellationToken.None);
        var card = JoinRolesCards.Settings(config, guildState.GetBot(guildId));

        await RespondAsync(InteractionCallback.ModifyMessage(m => m.Components = [card]));
    }
}

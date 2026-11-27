using JetBrains.Annotations;

using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace Wisp.Gateway.Features.JoinRoles.Modules;

/// <summary>
/// The role select on the <c>/join-roles</c> card. It saves on every change and redraws the card in place.
/// </summary>
[UsedImplicitly]
[RequireUserPermissions<RoleMenuInteractionContext>(Permissions.Administrator)]
public class JoinRolesComponentsModule(
    IJoinRolesSettingsService settingsService,
    IJoinRolesGuildState guildState) : ComponentInteractionModule<RoleMenuInteractionContext>
{
    [UsedImplicitly]
    [ComponentInteraction(JoinRolesCards.RolesMenuId)]
    public async Task SetRoles()
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var guildId = Context.Guild.Id;

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        var result = await settingsService.SetRolesAsync(guildId, [.. Context.SelectedValues.Select(r => r.Id)]);
        var card = JoinRolesCards.Settings(result.Config, guildState.GetBot(guildId), result.Rejected);

        await ModifyResponseAsync(m => m.Components = [card]);
    }
}

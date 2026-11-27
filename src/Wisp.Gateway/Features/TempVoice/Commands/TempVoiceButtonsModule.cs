using JetBrains.Annotations;

using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.TempVoice.Commands;

/// <summary>
/// The Enable / Disable button on the <c>/temp-voice</c> card. Both create or delete channels, which can outlast
/// Discord's 3-second reply window, so they defer first and redraw the card afterwards.
/// </summary>
[UsedImplicitly]
[RequireUserPermissions<ButtonInteractionContext>(Permissions.Administrator)]
public class TempVoiceButtonsModule(
    ITempVoiceSettingsService settingsService,
    ITempVoiceChannelsService channelsService) : ComponentInteractionModule<ButtonInteractionContext>
{
    [UsedImplicitly]
    [ComponentInteraction(TempVoiceCards.EnableId)]
    public Task Enable() => UpdateAsync(settingsService.EnableAsync);

    [UsedImplicitly]
    [ComponentInteraction(TempVoiceCards.DisableId)]
    public Task Disable() => UpdateAsync(settingsService.DisableAsync);

    private async Task UpdateAsync(Func<ulong, CancellationToken, Task<TempVoiceConfig>> action)
    {
        ArgumentNullException.ThrowIfNull(Context.Guild);
        var guildId = Context.Guild.Id;

        await RespondAsync(InteractionCallback.DeferredModifyMessage);

        var config = await action(guildId, CancellationToken.None);
        var activeChannels = await channelsService.CountActiveAsync(guildId);
        var card = TempVoiceCards.Settings(config, activeChannels);

        await ModifyResponseAsync(m => m.Components = [card]);
    }
}

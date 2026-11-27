using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord.Components;

namespace Wisp.Discord.Interactions;

public abstract class WispCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private bool _deferred;

    protected async Task DeferAsync(bool ephemeral = true)
    {
        await RespondAsync(InteractionCallback.DeferredMessage(ephemeral ? MessageFlags.Ephemeral : null));
        _deferred = true;
    }

    protected Task ReplyAsync(ComponentContainerProperties card, bool ephemeral = true)
    {
        if (_deferred)
        {
            return ModifyResponseAsync(x =>
            {
                x.WithFlags(MessageFlags.IsComponentsV2);
                x.WithComponents([card]);
            });
        }

        return RespondAsync(InteractionCallback.Message(card.ToMessage(ephemeral)));
    }
}
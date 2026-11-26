using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Wisp.Discord;

/// <summary>
/// Base for the bot's slash command modules. Use <see cref="DeferAsync"/> and <see cref="ReplyAsync"/> instead of
/// responding directly, so a command doesn't need to track whether it already deferred.
/// </summary>
public abstract class WispCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private bool _deferred;

    /// <summary>
    /// Acknowledges the interaction ("thinking…"). Call this before anything that can take longer than Discord's
    /// 3-second response window.
    /// </summary>
    protected async Task DeferAsync(bool ephemeral = true)
    {
        await RespondAsync(InteractionCallback.DeferredMessage(ephemeral ? MessageFlags.Ephemeral : null));
        _deferred = true;
    }

    /// <summary>
    /// Sends <paramref name="card"/> as the response, or edits the deferred one into it. After a defer,
    /// <paramref name="ephemeral"/> is ignored: the visibility chosen in <see cref="DeferAsync"/> stays.
    /// </summary>
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
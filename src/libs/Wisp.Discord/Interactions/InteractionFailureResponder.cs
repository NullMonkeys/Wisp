using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;
using NetCord.Services;

using Wisp.Discord.Components;

namespace Wisp.Discord.Interactions;

/// <summary>
/// Turns a failed command or component interaction into an error card for the user.
/// </summary>
internal static class InteractionFailureResponder
{
    public static async ValueTask RespondAsync(IFailResult failResult, Interaction interaction, string name, ILogger logger)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];

        ComponentContainerProperties card;

        if (failResult is IExceptionResult exceptionResult)
        {
            logger.LogError(exceptionResult.Exception,
                "Interaction `{Interaction}` failed for user {UserId} in guild {GuildId} (error ID {ErrorId})",
                name, interaction.User.Id, interaction.GuildId, errorId);

            card = Cards.Error("Something went wrong",
                $"An unexpected error occurred while running `{name}`.\nPlease try again in a moment.",
                $"Error ID: `{errorId}` • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>");
        }
        else
        {
            card = Cards.Warning("Can't do that", failResult.Message);
        }

        var response = card.ToMessage();

        try
        {
            await interaction.SendResponseAsync(InteractionCallback.Message(response));
        }
        catch (RestException)
        {
            // already responded or deferred
            try
            {
                await interaction.SendFollowupMessageAsync(response);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send error message for interaction `{Interaction}`", name);
            }
        }
    }
}

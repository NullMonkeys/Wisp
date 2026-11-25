using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace Wisp.Discord;

public sealed class ApplicationCommandResultHandler<TContext> : IApplicationCommandResultHandler<TContext>
    where TContext : IApplicationCommandContext
{
    public async ValueTask HandleResultAsync(
        IExecutionResult result,
        TContext context,
        GatewayClient? client,
        ILogger logger,
        IServiceProvider services)
    {
        if (result is not IFailResult failResult) return;

        var interaction = context.Interaction;
        var errorId = Guid.NewGuid().ToString("N")[..8];

        ComponentContainerProperties card;

        if (failResult is IExceptionResult exceptionResult)
        {
            logger.LogError(exceptionResult.Exception,
                "Command `{Command}` failed for user {UserId} in guild {GuildId} (error ID {ErrorId})",
                interaction.Data.Name, interaction.User.Id, interaction.GuildId, errorId);

            card = Cards.Error("Something went wrong",
                $"An unexpected error occurred while running `/{interaction.Data.Name}`.\nPlease try again in a moment.",
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
                logger.LogWarning(ex, "Failed to send error message for command `{Command}`", interaction.Data.Name);
            }
        }
    }
}
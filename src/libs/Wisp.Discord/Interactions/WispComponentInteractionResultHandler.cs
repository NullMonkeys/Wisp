using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace Wisp.Discord.Interactions;

public sealed class WispComponentInteractionResultHandler<TContext> : IComponentInteractionResultHandler<TContext>
    where TContext : IComponentInteractionContext
{
    public async ValueTask HandleResultAsync(
        IExecutionResult result,
        TContext context,
        GatewayClient? client,
        ILogger logger,
        IServiceProvider services)
    {
        if (result is not IFailResult failResult) return;

        await InteractionFailureResponder.RespondAsync(failResult, context.Interaction, context.Interaction.Data.CustomId, logger);
    }
}

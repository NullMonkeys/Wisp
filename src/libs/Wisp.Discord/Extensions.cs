using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

using Wisp.Discord.HealthChecks;
using Wisp.Discord.Interactions;
using Wisp.Discord.Rest.Proxy;

namespace Wisp.Discord;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddDiscordGateway()
        {
            var restProxy = builder.Configuration.GetValue<Uri?>("Discord:RestProxy");

            builder.Services.AddDiscordShardedGateway(options =>
            {
                options.Token = builder.Configuration.GetValue<string?>("Discord:Token");
                options.Intents = GatewayIntents.All;

                var shardRangeStart = builder.Configuration.GetValue("Discord:ShardRange:Start", 0);
                var shardRangeEnd = builder.Configuration.GetValue("Discord:ShardRange:End", 1);

                options.TotalShardCount = builder.Configuration.GetValue("Discord:TotalShardCount", 1);
                options.ShardRange = new Range(shardRangeStart, shardRangeEnd);

                if (restProxy is not null)
                {
                    options.RestClientConfiguration = new RestClientConfiguration
                    {
                        RequestHandler = new RestProxyRequestHandler(restProxy),
                        RateLimitManager = new RestProxyRateLimitManager(),
                    };
                }
            });

            builder.Services.AddShardedGatewayHandlers(Assembly.GetCallingAssembly(), ServiceLifetime.Scoped);
            builder.Services.AddApplicationCommands(options =>
            {
                options.ResultHandler = new WispApplicationCommandResultHandler<ApplicationCommandContext>();
            });
            builder.Services.AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>(options =>
            {
                options.ResultHandler = new WispComponentInteractionResultHandler<RoleMenuInteractionContext>();
            });
            builder.Services.AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>(options =>
            {
                options.ResultHandler = new WispComponentInteractionResultHandler<ButtonInteractionContext>();
            });

            builder.Services.AddHealthChecks()
                .AddCheck<DiscordGatewayHealthCheck>("discord-gateway");

            return builder;
        }
    }
}
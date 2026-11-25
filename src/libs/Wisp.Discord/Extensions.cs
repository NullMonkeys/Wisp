using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;

using Wisp.Discord.HealthChecks;

namespace Wisp.Discord;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddDiscordGateway()
        {
            builder.Services.AddDiscordShardedGateway(options =>
            {
                options.Token = builder.Configuration.GetValue<string?>("Discord:Token");
                options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates;
                
                var shardRangeStart = builder.Configuration.GetValue("Discord:ShardRange:Start", 0);
                var shardRangeEnd = builder.Configuration.GetValue("Discord:ShardRange:End", 1);
                
                options.TotalShardCount = builder.Configuration.GetValue("Discord:TotalShardCount", 1);
                options.ShardRange = new Range(shardRangeStart, shardRangeEnd);
            });

            builder.Services.AddShardedGatewayHandlers(Assembly.GetCallingAssembly(), ServiceLifetime.Scoped);
            builder.Services.AddApplicationCommands(options =>
            {
                options.ResultHandler = new ApplicationCommandResultHandler<ApplicationCommandContext>();
            });

            builder.Services.AddHealthChecks()
                .AddCheck<DiscordGatewayHealthCheck>("discord-gateway");
            
            return builder;
        }
    }

    extension(Color)
    {
        public static Color Error => new(237, 66, 69);
        public static Color Warning => new(254, 231, 92);
        public static Color Success => new(87, 242, 135);
    }
}
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
        /// <summary>
        /// Connects to Discord using <c>Discord:Token</c>, with the Guilds and GuildVoiceStates intents, and registers
        /// slash commands, the command error handler and the <c>discord-gateway</c> health check.
        /// </summary>
        /// <remarks>
        /// Gateway event handlers are found in the <em>calling</em> assembly and resolved per event in their own DI
        /// scope, so call this from the app's own project.
        /// <para>
        /// Sharding is optional: this instance runs shards <c>Discord:ShardRange:Start</c> up to, but not including,
        /// <c>Discord:ShardRange:End</c>, out of <c>Discord:TotalShardCount</c>. The defaults run a single shard.
        /// </para>
        /// </remarks>
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
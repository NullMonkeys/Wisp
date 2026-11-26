using Microsoft.Extensions.Diagnostics.HealthChecks;

using NetCord.Gateway;

namespace Wisp.Discord.HealthChecks;

/// <summary>
/// Healthy only when every shard this instance runs is connected and ready.
/// </summary>
internal sealed class DiscordGatewayHealthCheck(ShardedGatewayClient client) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var ready = client.Count(shard => shard.Status == WebSocketStatus.Ready);
        var description = $"{ready}/{client.Count} shards ready";

        return Task.FromResult(client.Count > 0 && ready == client.Count
            ? HealthCheckResult.Healthy(description)
            : HealthCheckResult.Unhealthy(description));
    }
}

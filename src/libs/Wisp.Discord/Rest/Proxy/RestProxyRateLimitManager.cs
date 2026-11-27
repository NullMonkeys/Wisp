using System.Collections.Concurrent;

using NetCord.Rest.RateLimits;

namespace Wisp.Discord.Rest.Proxy;


public sealed class RestProxyRateLimitManager : IRateLimitManager
{
    private const int MaxTrackedRoutes = 1024;

    private readonly ConcurrentDictionary<Route, long> _routeResets = new();
    private long _globalReset;

    public ValueTask<IGlobalRateLimiter> GetGlobalRateLimiterAsync(CancellationToken cancellationToken = default)
        => new(new GlobalGate(this));

    public ValueTask<IRouteRateLimiter> GetRouteRateLimiterAsync(Route route, CancellationToken cancellationToken = default)
        => new(new RouteGate(this, route));

    public ValueTask ExchangeRouteRateLimiterAsync(
        Route route,
        RateLimitInfo? rateLimitInfo,
        BucketInfo? previousBucketInfo,
        CancellationToken cancellationToken = default)
    {
        if (rateLimitInfo is not { Remaining: 0 })
        {
            return default;
        }

        _routeResets[route] = rateLimitInfo.Reset;
        PruneExpired();
        return default;
    }

    public void Dispose()
    {
    }

    private void PruneExpired()
    {
        if (_routeResets.Count <= MaxTrackedRoutes)
            return;

        var now = Environment.TickCount64;
        foreach (var entry in _routeResets)
        {
            if (entry.Value <= now)
                _routeResets.TryRemove(entry);
        }
    }

    private static RateLimitAcquisitionResult Acquire(long reset)
    {
        var wait = reset - Environment.TickCount64;
        return wait > 0
            ? RateLimitAcquisitionResult.RateLimit((int)Math.Min(wait, int.MaxValue))
            : RateLimitAcquisitionResult.NoRateLimit;
    }

    private sealed class GlobalGate(RestProxyRateLimitManager manager) : IGlobalRateLimiter
    {
        public ValueTask<RateLimitAcquisitionResult> TryAcquireAsync(CancellationToken cancellationToken = default)
            => new(Acquire(Interlocked.Read(ref manager._globalReset)));

        public ValueTask IndicateRateLimitAsync(long reset, CancellationToken cancellationToken = default)
        {
            long current;
            do
            {
                current = Interlocked.Read(ref manager._globalReset);
                if (reset <= current)
                    break;
            } while (Interlocked.CompareExchange(ref manager._globalReset, reset, current) != current);

            return default;
        }
    }

    private sealed class RouteGate(RestProxyRateLimitManager manager, Route route) : IRouteRateLimiter
    {
        public bool HasBucketInfo => true;

        public BucketInfo? BucketInfo => null;

        public ValueTask<RateLimitAcquisitionResult> TryAcquireAsync(CancellationToken cancellationToken = default)
        {
            if (!manager._routeResets.TryGetValue(route, out var reset))
                return new ValueTask<RateLimitAcquisitionResult>(RateLimitAcquisitionResult.NoRateLimit);

            var result = Acquire(reset);
            if (!result.RateLimited)
                manager._routeResets.TryRemove(new KeyValuePair<Route, long>(route, reset));

            return new ValueTask<RateLimitAcquisitionResult>(result);
        }

        public ValueTask CancelAcquireAsync(long acquisitionTimestamp, CancellationToken cancellationToken = default)
            => default;

        public ValueTask UpdateAsync(RateLimitInfo rateLimitInfo, CancellationToken cancellationToken = default)
            => default;

        public ValueTask IndicateExchangeAsync(long timestamp, CancellationToken cancellationToken = default)
            => default;
    }
}

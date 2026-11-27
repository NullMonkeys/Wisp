using NetCord.Rest.RateLimits;

namespace Wisp.Discord.Rest.Proxy;

public sealed class RestProxyRateLimitManager : IRateLimitManager
{
    public ValueTask<IGlobalRateLimiter> GetGlobalRateLimiterAsync(CancellationToken cancellationToken = default)
        => new(Unlimited.Instance);

    public ValueTask<IRouteRateLimiter> GetRouteRateLimiterAsync(Route route, CancellationToken cancellationToken = default)
        => new(Unlimited.Instance);

    public ValueTask ExchangeRouteRateLimiterAsync(
        Route route,
        RateLimitInfo? rateLimitInfo,
        BucketInfo? previousBucketInfo,
        CancellationToken cancellationToken = default)
        => default;

    public void Dispose()
    {
    }

    // mirrors NetCord's internal NoRateLimitRouteRateLimiter
    private sealed class Unlimited : IGlobalRateLimiter, IRouteRateLimiter
    {
        public static readonly Unlimited Instance = new();

        public bool HasBucketInfo => true;

        public BucketInfo? BucketInfo => null;

        public ValueTask<RateLimitAcquisitionResult> TryAcquireAsync(CancellationToken cancellationToken = default)
            => new(RateLimitAcquisitionResult.NoRateLimit);

        public ValueTask IndicateRateLimitAsync(long reset, CancellationToken cancellationToken = default) => default;

        public ValueTask CancelAcquireAsync(long acquisitionTimestamp, CancellationToken cancellationToken = default)
            => default;

        public ValueTask UpdateAsync(RateLimitInfo rateLimitInfo, CancellationToken cancellationToken = default)
            => default;

        public ValueTask IndicateExchangeAsync(long timestamp, CancellationToken cancellationToken = default) => default;
    }
}
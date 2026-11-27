using System.Diagnostics;
using System.Net;
using System.Text;

using NetCord;
using NetCord.Rest;

using Wisp.Discord.Rest;
using Wisp.Discord.Rest.Proxy;

namespace Wisp.Gateway.Tests.Discord;

public sealed class RestProxyTests
{
    private const string Token = "MTIz.fake.token";

    [Fact]
    public async Task Requests_go_to_the_proxy_with_the_bot_token()
    {
        var discord = new StubDiscord(UnknownChannel);
        using var client = CreateClient(discord);

        var deleted = await client.DeleteChannelIfExistsAsync(42, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        var request = Assert.Single(discord.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("http://rest-proxy:3000/api/v10/channels/42", request.Uri.ToString());
        Assert.Equal($"Bot {Token}", request.Authorization);
    }

    [Fact]
    public async Task A_rate_limited_request_is_retried_once_the_bucket_resets()
    {
        var discord = new StubDiscord(RateLimited, UnknownChannel);
        using var client = CreateClient(discord);

        var deleted = await client.DeleteChannelIfExistsAsync(42, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Equal(2, discord.Requests.Count);
        // x-ratelimit-reset-after is 0.1s; a little slack for timer resolution
        Assert.InRange(Gap(discord), TimeSpan.FromMilliseconds(80), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_globally_rate_limited_request_waits_out_retry_after()
    {
        var discord = new StubDiscord(GloballyRateLimited, UnknownChannel);
        using var client = CreateClient(discord);

        var deleted = await client.DeleteChannelIfExistsAsync(42, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Equal(2, discord.Requests.Count);
        // retry-after is 1s
        Assert.InRange(Gap(discord), TimeSpan.FromMilliseconds(900), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("http://rest-proxy/discord")]
    [InlineData("ftp://rest-proxy")]
    [InlineData("rest-proxy:3000")]
    public void Rejects_proxy_urls_it_cannot_send_to(string proxy)
    {
        Assert.Throws<ArgumentException>(() => new RestProxyRequestHandler(new Uri(proxy, UriKind.RelativeOrAbsolute)));
    }

    private static RestClient CreateClient(StubDiscord discord) => new(new BotToken(Token), new RestClientConfiguration
    {
        RequestHandler = new RestProxyRequestHandler(new Uri("http://rest-proxy:3000"), discord),
        RateLimitManager = new RestProxyRateLimitManager(),
    });

    private static TimeSpan Gap(StubDiscord discord)
        => Stopwatch.GetElapsedTime(discord.Requests[0].At, discord.Requests[1].At);

    private static HttpResponseMessage UnknownChannel() => new(HttpStatusCode.NotFound)
    {
        Content = new StringContent("""{"message":"Unknown Channel","code":10003}""", Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage RateLimited()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{"message":"You are being rate limited.","retry_after":0.1,"global":false}""",
                Encoding.UTF8, "application/json"),
        };

        response.Headers.Add("x-ratelimit-scope", "user");
        response.Headers.Add("x-ratelimit-bucket", "abc");
        response.Headers.Add("x-ratelimit-limit", "5");
        response.Headers.Add("x-ratelimit-remaining", "0");
        response.Headers.Add("x-ratelimit-reset-after", "0.1");
        response.Headers.Add("retry-after", "1");

        return response;
    }

    private static HttpResponseMessage GloballyRateLimited()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{"message":"You are being rate limited.","retry_after":1,"global":true}""",
                Encoding.UTF8, "application/json"),
        };

        response.Headers.Add("x-ratelimit-global", "true");
        response.Headers.Add("x-ratelimit-scope", "global");
        response.Headers.Add("retry-after", "1");

        return response;
    }

    private sealed record SentRequest(HttpMethod Method, Uri Uri, string? Authorization, long At);

    private sealed class StubDiscord(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public List<SentRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new SentRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(),
                Stopwatch.GetTimestamp()));

            return Task.FromResult(responses[Math.Min(Requests.Count, responses.Length) - 1]());
        }
    }
}
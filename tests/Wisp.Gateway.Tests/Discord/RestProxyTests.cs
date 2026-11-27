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
    public async Task A_rate_limited_request_is_retried_through_the_proxy()
    {
        var discord = new StubDiscord(RateLimited, UnknownChannel);
        using var client = CreateClient(discord);

        var deleted = await client.DeleteChannelIfExistsAsync(42, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Equal(2, discord.Requests.Count);
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

    private sealed record SentRequest(HttpMethod Method, Uri Uri, string? Authorization);

    private sealed class StubDiscord(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public List<SentRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new SentRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString()));

            return Task.FromResult(responses[Math.Min(Requests.Count, responses.Length) - 1]());
        }
    }
}
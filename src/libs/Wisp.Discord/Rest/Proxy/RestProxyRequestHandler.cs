using NetCord.Rest;

namespace Wisp.Discord.Rest.Proxy;

/// <summary>
/// Sends NetCord's REST requests to twilight http-proxy instead of Discord
/// </summary>
/// <remarks>
/// NetCord always builds <c>https://discord.com/api/v10/...</c>, so only the scheme, host and port are swapped. The
/// proxy accepts Discord's paths as they are.
/// </remarks>
public sealed class RestProxyRequestHandler : IRestRequestHandler
{
    // proxy holds a request until its rate limit resets, so the timeout has to sit that out
    // a hung proxy still fails eventually
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    private readonly Uri _proxy;
    private readonly HttpClient _httpClient;

    public RestProxyRequestHandler(Uri proxy) : this(proxy, new SocketsHttpHandler())
    {
    }

    public RestProxyRequestHandler(Uri proxy, HttpMessageHandler handler)
    {
        if (!proxy.IsAbsoluteUri || proxy.Scheme is not ("http" or "https") || proxy.AbsolutePath != "/")
        {
            throw new ArgumentException(
                $"The REST proxy must be an http(s) URL without a path, like 'http://rest-proxy', but was '{proxy}'.",
                nameof(proxy));
        }

        _proxy = proxy;
        _httpClient = new HttpClient(handler) { Timeout = RequestTimeout };
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.RequestUri = new UriBuilder(request.RequestUri!)
        {
            Scheme = _proxy.Scheme,
            Host = _proxy.Host,
            Port = _proxy.Port,
        }.Uri;

        return _httpClient.SendAsync(request, cancellationToken);
    }

    public void AddDefaultHeader(string name, IEnumerable<string> values)
        => _httpClient.DefaultRequestHeaders.Add(name, values);

    public void Dispose() => _httpClient.Dispose();
}
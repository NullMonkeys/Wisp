using System.Net;

using NetCord.Rest;

namespace Wisp.Discord.Rest;

public static class RestClientExtensions
{
    extension(RestClient restClient)
    {
        /// <summary>
        /// Deletes a channel, treating "it's already gone" as success.
        /// </summary>
        /// <returns><c>false</c> if the channel didn't exist.</returns>
        public async Task<bool> DeleteChannelIfExistsAsync(ulong channelId, CancellationToken ct = default)
        {
            try
            {
                await restClient.DeleteChannelAsync(channelId, cancellationToken: ct);
                return true;
            }
            catch (RestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}

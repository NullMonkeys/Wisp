using NetCord.Rest;

using Wisp.Discord.Rest;

namespace Wisp.Gateway.Features.TempVoice;

internal sealed class TempVoiceDiscord(RestClient restClient) : ITempVoiceDiscord
{
    public async Task<ulong> CreateChannelAsync(ulong guildId, GuildChannelProperties channel, CancellationToken ct = default)
    {
        var created = await restClient.CreateGuildChannelAsync(guildId, channel, cancellationToken: ct);
        return created.Id;
    }

    public async Task<bool> ChannelExistsAsync(ulong channelId, CancellationToken ct = default)
    {
        try
        {
            await restClient.GetChannelAsync(channelId, cancellationToken: ct);
            return true;
        }
        catch (RestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<bool> DeleteChannelIfExistsAsync(ulong channelId, CancellationToken ct = default)
        => restClient.DeleteChannelIfExistsAsync(channelId, ct);

    public Task MoveUserAsync(ulong guildId, ulong userId, ulong channelId, CancellationToken ct = default)
        => restClient.ModifyGuildUserAsync(guildId, userId, u => u.ChannelId = channelId, cancellationToken: ct);

    public Task SetPermissionOverwritesAsync(ulong channelId, IEnumerable<PermissionOverwriteProperties> overwrites,
        CancellationToken ct = default)
        => restClient.ModifyGuildChannelAsync(channelId, c => c.PermissionOverwrites = overwrites, cancellationToken: ct);

    public Task SendMessageAsync(ulong channelId, MessageProperties message, CancellationToken ct = default)
        => restClient.SendMessageAsync(channelId, message, cancellationToken: ct);
}

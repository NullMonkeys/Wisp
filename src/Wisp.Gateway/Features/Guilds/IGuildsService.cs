namespace Wisp.Gateway.Features.Guilds;

public interface IGuildsService
{
    Task EnsureGuildAsync(ulong id, string name, CancellationToken ct = default);
    Task DeleteGuildAsync(ulong id, CancellationToken ct = default);
}
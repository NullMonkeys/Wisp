using Microsoft.EntityFrameworkCore;

using Wisp.Persistence;

namespace Wisp.Gateway.Features.Guilds;

internal sealed partial class GuildsService(
    ILogger<GuildsService> logger,
    WispDbContext dbContext) : IGuildsService
{
    public async Task EnsureGuildAsync(ulong id, string name, CancellationToken ct = default)
    {
        var affected = await dbContext
            .Database
            .ExecuteSqlInterpolatedAsync($"""
                                          INSERT INTO guilds (id, name, created_at, updated_at)
                                          VALUES ({(long)id}, {name}, now(), now())
                                          ON CONFLICT (id) DO UPDATE
                                          SET name = excluded.name, updated_at = now()
                                          WHERE guilds.name IS DISTINCT FROM excluded.name
                                          """, ct);

        if (affected > 0)
            LogGuildSaved(id, name);
    }

    public async Task DeleteGuildAsync(ulong id, CancellationToken ct = default)
    {
        var affected = await dbContext
            .Guilds
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);

        if (affected > 0)
            LogGuildDeleted(id);
    }
    
    [LoggerMessage(LogLevel.Information, "Guild {Id} saved with name {Name}")]
    partial void LogGuildSaved(ulong id, string name);

    [LoggerMessage(LogLevel.Information, "Guild {Id} deleted")]
    partial void LogGuildDeleted(ulong id);
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Wisp.Persistence;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddPersistence()
        {
            builder.AddNpgsqlDbContext<WispDbContext>(
                connectionName: "database",
                configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());
            
            return builder;
        }
    }

    extension<THost>(THost host) where THost : IHost
    {
        public async Task<IHost> MigrateDatabaseAsync()
        {
            await using var scope = host.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WispDbContext>();
            await dbContext.Database.MigrateAsync();
            return host;
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Wisp.Persistence;

public static class Extensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Registers <see cref="WispDbContext"/> against the <c>database</c> connection string, with snake_case
        /// table and column names.
        /// </summary>
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
        /// <summary>
        /// Applies pending EF Core migrations. Runs at startup, before the bot connects.
        /// </summary>
        public async Task<IHost> MigrateDatabaseAsync()
        {
            await using var scope = host.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WispDbContext>();
            await dbContext.Database.MigrateAsync();
            return host;
        }
    }
}
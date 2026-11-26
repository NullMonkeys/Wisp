using System.Reflection;

using Microsoft.EntityFrameworkCore;

using Wisp.Persistence.Common;
using Wisp.Persistence.Entities;

namespace Wisp.Persistence;

/// <summary>
/// The bot's database. Discord IDs are <see cref="ulong"/> in code but stored as <c>bigint</c> (Postgres has no
/// unsigned integers), and entity configurations are picked up from this assembly automatically.
/// </summary>
public sealed class WispDbContext(DbContextOptions<WispDbContext> options) : DbContext(options)
{
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<TempVoiceConfig> TempVoiceConfigs => Set<TempVoiceConfig>();
    public DbSet<TempChannel> TempChannels => Set<TempChannel>();
    public DbSet<TempVoicePreferences> TempVoicePreferences => Set<TempVoicePreferences>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<ulong>().HaveConversion<long>();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TouchModifiedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        TouchModifiedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void TouchModifiedEntities()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.Touch();
        }
    }
}

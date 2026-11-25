using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wisp.Persistence.Entities;

namespace Wisp.Persistence.Configurations;

public sealed class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.ToTable("guilds");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).HasMaxLength(100);
    }
}

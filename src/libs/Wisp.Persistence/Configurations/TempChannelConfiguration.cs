using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wisp.Persistence.Entities;

namespace Wisp.Persistence.Configurations;

internal sealed class TempChannelConfiguration : IEntityTypeConfiguration<TempChannel>
{
    public void Configure(EntityTypeBuilder<TempChannel> builder)
    {
        builder.ToTable("temp_channels");
        builder.HasKey(x => x.ChannelId);
        builder.Property(x => x.ChannelId).ValueGeneratedNever();
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => new { x.GuildId, x.OwnerId }).IsUnique();
        
        builder
            .HasOne(x => x.Guild)
            .WithMany()
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
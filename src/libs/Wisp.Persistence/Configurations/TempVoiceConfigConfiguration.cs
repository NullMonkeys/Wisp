using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wisp.Persistence.Entities;

namespace Wisp.Persistence.Configurations;

internal sealed class TempVoiceConfigConfiguration : IEntityTypeConfiguration<TempVoiceConfig>
{
    public void Configure(EntityTypeBuilder<TempVoiceConfig> builder)
    {
        builder.ToTable("temp_voice_configs", t => t.HasCheckConstraint(
            "ck_temp_voice_configs_enabled_requires_channels",
            "NOT enabled OR (category_id IS NOT NULL AND master_channel_id IS NOT NULL)"));
        
        builder.HasKey(x => x.GuildId);
        builder.HasIndex(x => x.MasterChannelId).IsUnique();

        builder
            .HasOne(x => x.Guild)
            .WithOne()
            .HasForeignKey<TempVoiceConfig>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
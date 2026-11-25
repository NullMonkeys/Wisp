using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wisp.Persistence.Entities;

namespace Wisp.Persistence.Configurations;

internal sealed class TempVoicePreferencesConfiguration : IEntityTypeConfiguration<TempVoicePreferences>
{
    public void Configure(EntityTypeBuilder<TempVoicePreferences> builder)
    {
        builder.ToTable("temp_voice_preferences");
        builder.HasKey(x => new { x.GuildId, x.UserId });

        builder.Property(x => x.Name).HasMaxLength(100);
        builder.OwnsMany(x => x.Overwrites, b => b.ToJson());
        
        builder
            .HasOne<Guild>()
            .WithMany()
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
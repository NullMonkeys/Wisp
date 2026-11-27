using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Wisp.Persistence.Entities;

namespace Wisp.Persistence.Configurations;

internal sealed class JoinRolesConfigConfiguration : IEntityTypeConfiguration<JoinRolesConfig>
{
    public void Configure(EntityTypeBuilder<JoinRolesConfig> builder)
    {
        builder.ToTable("join_roles_configs", t => t.HasCheckConstraint(
            "ck_join_roles_configs_role_limit",
            $"cardinality(role_ids) <= {JoinRolesConfig.MaxRoles}"));

        builder.HasKey(x => x.GuildId);

        builder.PrimitiveCollection(x => x.RoleIds).ElementType().HasConversion<long>();

        builder
            .HasOne(x => x.Guild)
            .WithOne()
            .HasForeignKey<JoinRolesConfig>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

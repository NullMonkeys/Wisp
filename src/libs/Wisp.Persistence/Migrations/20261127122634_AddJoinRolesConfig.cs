using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wisp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinRolesConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "join_roles_configs",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    role_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_join_roles_configs", x => x.guild_id);
                    table.CheckConstraint("ck_join_roles_configs_role_limit", "cardinality(role_ids) <= 25");
                    table.ForeignKey(
                        name: "fk_join_roles_configs_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "join_roles_configs");
        }
    }
}

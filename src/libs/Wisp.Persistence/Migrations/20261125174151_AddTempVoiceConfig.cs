using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wisp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTempVoiceConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "temp_voice_configs",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    master_channel_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temp_voice_configs", x => x.guild_id);
                    table.CheckConstraint("ck_temp_voice_configs_enabled_requires_channels", "NOT enabled OR (category_id IS NOT NULL AND master_channel_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_temp_voice_configs_guilds_guild_id",
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
                name: "temp_voice_configs");
        }
    }
}

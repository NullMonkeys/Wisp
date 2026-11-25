using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wisp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTempChannelsAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "temp_channels",
                columns: table => new
                {
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temp_channels", x => x.channel_id);
                    table.ForeignKey(
                        name: "fk_temp_channels_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "temp_voice_preferences",
                columns: table => new
                {
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_limit = table.Column<int>(type: "integer", nullable: true),
                    bitrate = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    overwrites = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temp_voice_preferences", x => new { x.guild_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_temp_voice_preferences_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_temp_voice_configs_master_channel_id",
                table: "temp_voice_configs",
                column: "master_channel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_temp_channels_guild_id",
                table: "temp_channels",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_temp_channels_guild_id_owner_id",
                table: "temp_channels",
                columns: new[] { "guild_id", "owner_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "temp_channels");

            migrationBuilder.DropTable(
                name: "temp_voice_preferences");

            migrationBuilder.DropIndex(
                name: "ix_temp_voice_configs_master_channel_id",
                table: "temp_voice_configs");
        }
    }
}

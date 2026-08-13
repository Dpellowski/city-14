using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres._Misfits
{
    /// <inheritdoc />
    public partial class MisfitsCivicPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_civic_point_change",
                columns: table => new
                {
                    character_civic_point_change_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    delta = table.Column<long>(type: "bigint", nullable: false),
                    balance_after = table.Column<long>(type: "bigint", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_civic_point_change", x => x.character_civic_point_change_id);
                    table.ForeignKey(
                        name: "FK_character_civic_point_change_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_civic_points",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false, defaultValue: 50L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_civic_points", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_character_civic_points_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                "INSERT INTO character_civic_points (profile_id, points) " +
                "SELECT profile_id, 50 FROM profile " +
                "ON CONFLICT (profile_id) DO NOTHING;");

            migrationBuilder.CreateIndex(
                name: "IX_character_civic_point_change_profile_id_source_kind_source_~",
                table: "character_civic_point_change",
                columns: new[] { "profile_id", "source_kind", "source_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_civic_point_change");

            migrationBuilder.DropTable(
                name: "character_civic_points");
        }
    }
}

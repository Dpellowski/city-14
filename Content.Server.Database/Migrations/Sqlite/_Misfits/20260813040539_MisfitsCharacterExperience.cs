using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite._Misfits
{
    /// <inheritdoc />
    public partial class MisfitsCharacterExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_experience",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    experience_group = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    total_experience = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_experience", x => new { x.profile_id, x.experience_group });
                    table.CheckConstraint("TotalExperienceNonNegative", "total_experience >= 0");
                    table.ForeignKey(
                        name: "FK_character_experience_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_experience");
        }
    }
}

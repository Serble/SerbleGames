using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SerbleGames.Backend.Migrations
{
    /// <inheritdoc />
    public partial class GameReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinuxBuild",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MacBuild",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Public",
                table: "Games",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WindowsBuild",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinuxBuild",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MacBuild",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Public",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "WindowsBuild",
                table: "Games");
        }
    }
}

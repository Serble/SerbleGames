using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SerbleGames.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaytimeToOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPlayed",
                table: "GameOwnerships",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Playtime",
                table: "GameOwnerships",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPlayed",
                table: "GameOwnerships");

            migrationBuilder.DropColumn(
                name: "Playtime",
                table: "GameOwnerships");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PreSportConnectUserDatabaseMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TournamentRole",
                table: "Participations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TournamentRole",
                table: "Participations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

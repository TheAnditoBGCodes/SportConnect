using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixTableIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TournamentId",
                table: "Tournaments",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Tournaments",
                newName: "TournamentId");
        }
    }
}

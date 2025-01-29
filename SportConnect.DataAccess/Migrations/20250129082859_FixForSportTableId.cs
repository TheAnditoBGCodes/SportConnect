using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportConnect.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixForSportTableId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SportId",
                table: "Sports",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Sports",
                newName: "SportId");
        }
    }
}

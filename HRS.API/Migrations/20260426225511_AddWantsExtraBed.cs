using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWantsExtraBed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WantsExtraBed",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WantsExtraBed",
                table: "Reservations");
        }
    }
}

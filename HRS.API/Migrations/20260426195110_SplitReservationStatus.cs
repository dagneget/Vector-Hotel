using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.API.Migrations
{
    /// <inheritdoc />
    public partial class SplitReservationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Reservations",
                newName: "RoomStatus");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "RoomStatus",
                table: "Reservations",
                newName: "Status");
        }
    }
}
